# Отчёт до исправления

## 1. Паспорт проверки

| Поле | Значение |
| --- | --- |
| Автор и личный вклад | Марьин Даниил: включение негативного теста, запуск анализаторов, воспроизведение дефекта и интерпретация сигналов; [PR #1](https://github.com/DanyKaystery/s09-sast-secrets/pull/1) |
| Объект проверки | Commit `ebb15a1b4bfecefbf2b3580e54b66d62da50e763`, рабочее дерево чистое |
| Манифест объекта | [source-before.json](actual/run-42628309579/source-before.json) |
| Инструмент SAST | Semgrep CE 1.175.0 |
| Образ SAST | `semgrep/semgrep:1.175.0@sha256:b94b53d02fd4a022f9eac4e2af1380f5c3c4c21400e79d3336bdff1d1db5e796` |
| Инструмент поиска секретов | Gitleaks 8.30.1 |
| Образ поиска секретов | `ghcr.io/gitleaks/gitleaks:v8.30.1@sha256:c00b6bd0aeb3071cbcb79009cb16a60dd9e0a7c60e2be9ab65d25e6bc8abbb7f` |
| Конфигурация SAST | `semgrep.yml`; область — каталог `src`; локальные правила, метрики выключены |
| Конфигурация secrets scan | `.gitleaks-training.toml`; отдельно текущее дерево и вся история `fixtures/secret-history.bundle` |
| Запуск | [GitHub Actions 42628309579](https://github.com/DanyKaystery/s09-sast-secrets/actions/runs/42628309579) |

## 2. Конфигурация и воспроизведение

SAST:

```text
semgrep scan --config semgrep.yml --metrics=off --disable-version-check --no-git-ignore --json --output /out/semgrep-before.json src
```

Негативная и регрессионные проверки:

```text
dotnet test NotesApi.slnx --no-restore --logger "trx;LogFileName=before.trx"
```

Поиск секретов:

```text
gitleaks dir /history --config .gitleaks-training.toml --redact --report-format json
gitleaks git /history --log-opts=--all --config .gitleaks-training.toml --redact --report-format json
```

## 3. Первичные результаты

| Проверка | Exit code | Результат | Первичный артефакт |
| --- | ---: | --- | --- |
| Semgrep | 0 | 8 сигналов, ошибок анализатора нет | [semgrep-before.json](actual/run-42628309579/semgrep-before.json) |
| Тесты | 1 | 4 passed, 1 failed, 0 skipped | [tests-before.txt](actual/run-42628309579/tests-before.txt), [before.trx](actual/run-42628309579/tests-before/before.trx) |
| Gitleaks, текущее дерево | 0 | 0 сигналов | [gitleaks-tree.json](actual/run-42628309579/gitleaks-tree.json) |
| Gitleaks, полная история | 1 | 1 замаскированный учебный сигнал | [gitleaks-history.json](actual/run-42628309579/gitleaks-history.json) |

Exit code `1` у тестов означает воспроизведённое нарушение. Exit code `1` у Gitleaks означает обнаруженную запись, а не ошибку запуска.

## 4. Находки и интерпретация

### Сигналы Semgrep

| № | Rule ID | Файл и строка | Severity | Решение по сигналу |
| ---: | --- | --- | --- | --- |
| 1 | `sql-commandtext-assignment` | `NotesRepository.cs:27` | WARNING | Статический DDL; дефект не подтверждён |
| 2 | `sql-commandtext-assignment` | `NotesRepository.cs:35` | WARNING | Статический запрос подсчёта; дефект не подтверждён |
| 3 | `sql-commandtext-assignment` | `NotesRepository.cs:48` | WARNING | Связан с F-01: требует анализа потока данных |
| 4 | `sql-commandtext-concat` | `NotesRepository.cs:48` | ERROR | F-01 подтверждён: пользовательские данные меняют структуру SQL |
| 5 | `sql-commandtext-assignment` | `NotesRepository.cs:59` | WARNING | `Add` использует параметры; дефект не подтверждён |
| 6 | `weak-hash-md5` | `Signals.cs:16` | WARNING | MD5 применяется как ключ кеша, не как security-механизм; текущих вызовов нет |
| 7 | `password-field-literal` | `Signals.cs:23` | INFO | Ложное срабатывание: строка является именем поля, а не секретом |
| 8 | `insecure-random` | `Signals.cs:30` | WARNING | Небезопасно для токена, но метод недостижим из текущего API |

### F-01. Подтверждённая SQL-инъекция

HTTP-параметр `q` поступал в `NotesRepository.Search` и включался в `CommandText` конкатенацией. Значение `%' OR 1=1 -- ` превращало ограничение по владельцу в истинное условие для всех строк.

Негативный тест от имени Alice получил две записи: собственную заметку Alice и чужую заметку Bob. Проверка `Assert.DoesNotContain` завершилась ошибкой, поэтому нарушение изоляции подтверждено не только сигналом анализатора, но и наблюдаемым поведением.

### F-06. Сигнал Gitleaks

В текущем дереве отдельной тренировочной истории сигналов нет. При проверке всей истории найден `rbpo-training-token` в `demo-settings.env:1`, commit `34d695f7967849875928173c3d4c03c8dc20c52e`. Значение в отчёте замаскировано.

Это фиктивный токен из изолированной учебной истории. Для него не нужны отзыв полномочий и переписывание истории. При реальной утечке первым действием была бы ротация секрета и проверка последствий, а не простое удаление строки новым commit.

## 5. Решение и изменение

Для F-01 принято решение исключить конкатенацию: оставить структуру SQL статической, а `owner` и поисковый шаблон передавать параметрами. Затем нужно повторить тот же набор тестов и Semgrep с неизменной конфигурацией. Остальные сигналы не исправляются механически ради уменьшения счётчика: для них сохраняются контекстные решения в реестре.

## 6. Проверка и сравнение

Этот отчёт фиксирует исходную точку сравнения: 8 сигналов Semgrep и воспроизводимое падение одного негативного теста. Критерий успешного исправления — отсутствие чужой заметки при том же payload, сохранение штатных сценариев и исчезновение узкого `sql-commandtext-concat` при rerun.

## 7. Ограничения и остаток

Semgrep охватывает каталог `src` и пять учебных правил, а не все свойства C#-приложения. Gitleaks использует профиль, распознающий только фиктивный формат токена в отдельной истории. Результаты нельзя интерпретировать как полное доказательство безопасности приложения.

## 8. Итог

Подтверждён один достижимый дефект — SQL-инъекция с нарушением изоляции заметок. Два Semgrep-сигнала относятся к одной причине и учитываются как один finding. Остальные сигналы требуют отдельных решений по контексту, но не увеличивают число подтверждённых дефектов автоматически.
