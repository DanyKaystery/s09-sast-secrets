# Сохранённые результаты запуска 42628309579

Отчёты получены собственным запуском в форке: [GitHub Actions run 42628309579](https://github.com/DanyKaystery/s09-sast-secrets/actions/runs/42628309579). Анализ выполнялся на GitHub-hosted runner; локальные .NET, Docker и PowerShell не использовались.

## Происхождение

- версия «до»: `ebb15a1b4bfecefbf2b3580e54b66d62da50e763`;
- версия «после»: `f6f3475fafcee6ce8e39dc262c1c8d5b638131c8`;
- Semgrep CE 1.175.0, образ закреплён digest `sha256:b94b53d02fd4a022f9eac4e2af1380f5c3c4c21400e79d3336bdff1d1db5e796`;
- Gitleaks 8.30.1, образ закреплён digest `sha256:c00b6bd0aeb3071cbcb79009cb16a60dd9e0a7c60e2be9ab65d25e6bc8abbb7f`;
- .NET SDK 10.0.401.

## Содержимое

- `semgrep-before.json`, `semgrep-after.json` — полные отчёты SAST;
- `semgrep-*-summary.tsv` — компактные перечни сигналов;
- `tests-before.txt`, `tests-after.txt` и каталоги `tests-*` — текстовые и TRX-результаты тестов;
- `gitleaks-tree.json`, `gitleaks-history.json` — проверка текущего дерева и полной учебной истории;
- `source-before.json`, `source-after.json` — commits, состояние Git и SHA-256 проверенных файлов;
- `*-version.txt`, `*-exit-code.txt`, `dotnet-info.txt` — версии инструментов и коды завершения;
- `summary.txt` — числовая сводка.

Отчёт Gitleaks хранит найденное значение в замаскированном виде. Учебный токен в документации не публикуется.
