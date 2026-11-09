ОТЧЁТ ДО ИСПРАВЛЕНИЯ

Автор и вклад: Марьин Даниил. Включил негативный тест, выполнил сканирование и воспроизвёл дефект.

Объект: commit ebb15a1b4bfecefbf2b3580e54b66d62da50e763, рабочее дерево чистое. Манифест: reports/actual/run-42628309579/source-before.json.

Инструменты: Semgrep CE 1.175.0 и Gitleaks 8.30.1. Образы закреплены digest, указанными в .github/workflows/s09-evidence.yml.

Область: Semgrep проверяет src по semgrep.yml. Gitleaks проверяет текущее дерево и полную историю fixtures/secret-history.bundle по .gitleaks-training.toml. Запуск: https://github.com/DanyKaystery/s09-sast-secrets/actions/runs/42628309579.

Результаты: Semgrep завершился с exit code 0 и выдал 8 сигналов. Тесты завершились с exit code 1: 4 passed, 1 failed. Gitleaks нашёл 0 сигналов в текущем дереве и 1 замаскированный учебный токен в истории.

Подтверждённый finding: sql-commandtext-assignment и sql-commandtext-concat в src/NotesApi/NotesRepository.cs:48 относятся к одной причине. Параметры owner и q включались в SQL конкатенацией. Запрос %' OR 1=1 -- от имени Alice возвращал также заметку Bob, поэтому нарушение изоляции данных подтверждено негативным тестом.

Другие решения: статические и параметризованные SQL-присваивания не подтверждают отдельные дефекты; строка password является именем поля, а не секретом; MD5 используется как ключ кеша, а не как механизм защиты; System.Random небезопасен для токена, но метод не вызывается текущим API. Исторический токен Gitleaks фиктивный и не требует ротации.

Решение: заменить конкатенацию в Search на статический SQL с параметрами owner и pattern, затем повторить тот же тест и Semgrep без изменения правил и области.

Первичные свидетельства находятся в reports/actual/run-42628309579: semgrep-before.json, tests-before.txt, tests-before/before.trx, gitleaks-tree.json и gitleaks-history.json.

Ограничение: учебные правила проверяют только выбранные конструкции в src и не доказывают безопасность всего приложения.
