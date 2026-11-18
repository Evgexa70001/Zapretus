using System;
using System.Linq;
using System.Threading.Tasks;

namespace Zapretus {
    internal sealed partial class MainForm {
        async Task Scan() {
            if (engine.Running) { Log("Сначала выключите движок для подбора стратегий."); return; }
            if (System.Diagnostics.Process.GetProcessesByName("winws").Length > 0 || System.Diagnostics.Process.GetProcessesByName("winws2").Length > 0) {
                Log("Остановите другой zapret перед подбором."); return;
            }
            BeginWork(); scan.Text = "Отменить"; scanSummary.Text = "Подбор выполняется…";
            try {
                using (var report = new ScanReport(dataDir)) {
                    Log("Отчёт: " + report.Path);
                    Log("Две исходные проверки без обхода:");
                    var baseline = await Measure(report, "Без обхода", "baseline-1");
                    var second = await Measure(report, "Без обхода", "baseline-2");
                    // A candidate must beat both baseline observations, without regressions.
                    if (baseline.AllPassed && second.AllPassed) {
                        Log("Все измеряемые проверки уже проходят без обхода. Профиль не изменён.");
                        scanSummary.Text = "Тесты проходят без обхода";
                        if (String.IsNullOrEmpty(videoUrl)) Log("Добавьте ссылку на медиаданные для проверки загрузки видео.");
                        return;
                    }
                    Profile best = null; ProbeResult bestResult = null; int bestScore = Math.Max(baseline.Score, second.Score);
                    var candidates = profiles.Items.Cast<Profile>().OrderByDescending(p => p.Experimental).ToArray();
                    for (int i = 0; i < candidates.Length; i++) {
                        cancellation.Token.ThrowIfCancellationRequested();
                        Profile p = candidates[i]; Log("ПРОВЕРКА КАНДИДАТА [" + (i+1) + "/" + candidates.Length + "] " + p.ToString());
                        try {
                            await Start(p, cancellation.Token);
                            var result = await Measure(report, p.Name, "trial");
                            if (!engine.Running) throw new InvalidOperationException("Движок завершился во время проверки.");
                            if (!result.Improves(baseline) || !result.Improves(second)) continue;
                            if (result.Score <= bestScore) {
                                if (best != null && result.Score == bestScore)
                                    Log("Равный результат. Выбранным остаётся подтверждённый профиль: " + best.ToString());
                                continue;
                            }
                            // Restart so a second pass cannot reuse the first pass's sockets.
                            engine.Stop(); await Task.Delay(700, cancellation.Token);
                            await Start(p, cancellation.Token);
                            var confirm = await Measure(report, p.Name, "confirmation");
                            if (!engine.Running) throw new InvalidOperationException("Движок завершился при подтверждении.");
                            if (confirm.Improves(baseline) && confirm.Improves(second) && confirm.Preserves(result) && confirm.Score > bestScore) {
                                best = p; bestScore = Math.Min(result.Score, confirm.Score);
                                bestResult = confirm;
                                // Make each confirmed winner the default immediately, even if a later trial is cancelled.
                                profiles.SelectedItem = p; SaveProfile(); UpdateSelectedProfile(); profiles.Invalidate();
                                scanSummary.Text = "Выбран: " + p.ToString() + " · " + confirm.Summary;
                                Log("НОВЫЙ ВЫБОР ПО УМОЛЧАНИЮ: " + p.ToString() + " · " + confirm.Summary + ". Подтверждено повторной проверкой.");
                                if (result.AllPassed && confirm.AllPassed) break;
                            }
                        } catch (OperationCanceledException) { throw; }
                        catch (Exception e) { Log("Пропуск: " + e.Message); report.Error(p.Name, "trial-error", e.GetType().Name); }
                        finally { engine.Stop(); }
                        await Task.Delay(700, cancellation.Token);
                    }
                    cancellation.Token.ThrowIfCancellationRequested();
                    if (best != null) {
                        engine.Stop(); profiles.SelectedItem = best; SaveProfile();
                        UpdateSelectedProfile(); profiles.Invalidate();
                        scanSummary.Text = "Итог: " + best.ToString() + " · " + bestResult.Summary;
                        Log("ИТОГ ПОДБОРА: " + best.ToString() + " · " + bestResult.Summary + ". Выбран и сохранён по умолчанию.");
                        Log("При одинаковых баллах сохраняется первый подтверждённый вариант. Последний проверенный кандидат не обязательно лучший.");
                        Log("Нажмите кнопку питания, чтобы включить выбранный профиль.");
                        Log("Результат относится только к выполненным тестам; голос и QUIC не подтверждены.");
                    } else { scanSummary.Text = "Улучшение не найдено"; Log("ИТОГ ПОДБОРА: подтверждённого улучшения нет. Выбор по умолчанию не изменён: " + profiles.SelectedItem); }
                }
            } catch (OperationCanceledException) { scanSummary.Text = "Подбор отменён"; Log("Подбор отменён. Отчёт и последний выбранный профиль сохранены."); }
            catch (Exception e) { scanSummary.Text = "Ошибка подбора"; Log("Ошибка подбора: " + e.Message); }
            finally { engine.Stop(); EndWork(); }
        }
        async Task<ProbeResult> Measure(ScanReport report, string profile, string stage) {
            var result = await Probe.Run(Log, videoUrl, cancellation.Token);
            report.Add(profile, stage, result); Log(profile + ": " + result.Summary); return result;
        }
    }
}
