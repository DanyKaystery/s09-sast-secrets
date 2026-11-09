# AVC-01: изоляция заметок при поиске

- **Автор и личный вклад:** Марьин Даниил включил негативный тест, воспроизвёл нарушение, параметризовал SQL, выполнил повторные проверки и разобрал остаточные сигналы; [PR #1](https://github.com/DanyKaystery/s09-sast-secrets/pull/1).
- **Объект до:** commit `ebb15a1b4bfecefbf2b3580e54b66d62da50e763`, дерево чистое; [манифест и SHA-256](reports/actual/run-42628309579/source-before.json).
- **Инструмент/версия:** Semgrep CE 1.175.0; образ `semgrep/semgrep:1.175.0@sha256:b94b53d02fd4a022f9eac4e2af1380f5c3c4c21400e79d3336bdff1d1db5e796`.
- **Конфигурация/область:** локальный `semgrep.yml`; каталог `src`, C#-файлы; `--no-git-ignore`; метрики и проверка обновления выключены. Тесты, документация и fixture-история не входят в SAST-scope.
- **Воспроизведение:** из корня объекта выполнен `semgrep scan --config semgrep.yml --metrics=off --disable-version-check --no-git-ignore --json --output /out/semgrep-before.json src`, exit code 0; затем `dotnet test NotesApi.slnx --no-restore`, exit code 1 из-за ожидаемого падения негативного теста.
- **Первичный результат:** [Semgrep до](reports/actual/run-42628309579/semgrep-before.json), [тесты до](reports/actual/run-42628309579/tests-before.txt), [TRX до](reports/actual/run-42628309579/tests-before/before.trx).
- **Сигнал:** `sql-commandtext-assignment` и `sql-commandtext-concat`, `src/NotesApi/NotesRepository.cs:48`; два правила указывают на одну причину.
- **Интерпретация:** HTTP-параметр `q` и значение владельца достигали `CommandText` через конкатенацию. Payload `%' OR 1=1 -- ` отменял ограничение по владельцу; тест от имени Alice получал заметку Bob.
- **Решение:** finding подтверждён, высокий приоритет из-за нарушения конфиденциальности данных другого пользователя; исправить формирование SQL и сохранить негативную проверку.
- **Изменение или отклонение:** commit `f1ee86edfe55b56af7cca29089469b003fd943aa`; SQL стал статическим, `owner` и шаблон поиска передаются параметрами `$owner` и `$pattern`. Широкое правило не подавлено.
- **Объект после:** commit `f6f3475fafcee6ce8e39dc262c1c8d5b638131c8`, дерево чистое; [манифест и SHA-256](reports/actual/run-42628309579/source-after.json).
- **Проверки:** до исправления — 4 passed, 1 failed: Alice получила заметку Bob; после — 5 passed, 0 failed, включая негативный тест, штатный поиск и добавление текста с апострофом.
- **Rerun:** те же Semgrep 1.175.0, `semgrep.yml`, команда и scope; [результат после](reports/actual/run-42628309579/semgrep-after.json) содержит 7 вместо 8 сигналов, `sql-commandtext-concat` отсутствует; [тесты после](reports/actual/run-42628309579/tests-after.txt), [TRX после](reports/actual/run-42628309579/tests-after/after.trx).
- **Ограничения вывода и пересмотр:** цепочка подтверждает исправление конкретной SQL-инъекции и изоляцию владельцев в проверенном сценарии, но не безопасность всего приложения. Оставшийся `sql-commandtext-assignment` является широким сигналом безопасного параметризованного запроса и остаётся точкой ручного контроля.
