# Отчёт после исправления

## 1. Паспорт проверки

| Поле | Значение |
| --- | --- |
| Автор и личный вклад | Марьин Даниил: параметризация SQL, повторные тесты и анализ остаточных сигналов; [PR #1](https://github.com/DanyKaystery/s09-sast-secrets/pull/1) |
| Объект проверки | Commit `f6f3475fafcee6ce8e39dc262c1c8d5b638131c8`, рабочее дерево чистое |
| Commit исправления | `f1ee86edfe55b56af7cca29089469b003fd943aa` |
| Манифест объекта | [source-after.json](actual/run-42628309579/source-after.json) |
| Инструмент SAST | Semgrep CE 1.175.0 |
| Образ SAST | `semgrep/semgrep:1.175.0@sha256:b94b53d02fd4a022f9eac4e2af1380f5c3c4c21400e79d3336bdff1d1db5e796` |
| Инструмент поиска секретов | Gitleaks 8.30.1 |
| Образ поиска секретов | `ghcr.io/gitleaks/gitleaks:v8.30.1@sha256:c00b6bd0aeb3071cbcb79009cb16a60dd9e0a7c60e2be9ab65d25e6bc8abbb7f` |
| Конфигурация и область | Те же `semgrep.yml`, `.gitleaks-training.toml` и области, что в отчёте до исправления |
| Запуск | [GitHub Actions 42628309579](https://github.com/DanyKaystery/s09-sast-secrets/actions/runs/42628309579) |

## 2. Конфигурация и воспроизведение

Использованы те же команды, версии инструментов, правила и область, что в отчёте до исправления. Для SAST изменена только метка выходного файла на `after`; тесты записаны в `after.trx`.

```text
semgrep scan --config semgrep.yml --metrics=off --disable-version-check --no-git-ignore --json --output /out/semgrep-after.json src
dotnet test NotesApi.slnx --no-restore --logger "trx;LogFileName=after.trx"
```

## 3. Первичные результаты

| Проверка | Exit code | Результат | Первичный артефакт |
| --- | ---: | --- | --- |
| Semgrep | 0 | 7 сигналов, ошибок анализатора нет | [semgrep-after.json](actual/run-42628309579/semgrep-after.json) |
| Тесты | 0 | 5 passed, 0 failed, 0 skipped | [tests-after.txt](actual/run-42628309579/tests-after.txt), [after.trx](actual/run-42628309579/tests-after/after.trx) |
| Gitleaks, текущее дерево | 0 | 0 сигналов | [gitleaks-tree.json](actual/run-42628309579/gitleaks-tree.json) |
| Gitleaks, полная история | 1 | 1 замаскированный учебный сигнал; результат не связан с SQL-исправлением | [gitleaks-history.json](actual/run-42628309579/gitleaks-history.json) |

## 4. Находки и интерпретация

### Сигналы Semgrep после исправления

| № | Rule ID | Файл и строка | Severity | Решение по сигналу |
| ---: | --- | --- | --- | --- |
| 1 | `sql-commandtext-assignment` | `NotesRepository.cs:27` | WARNING | Статический DDL; сохранён без изменения |
| 2 | `sql-commandtext-assignment` | `NotesRepository.cs:35` | WARNING | Статический запрос подсчёта; сохранён без изменения |
| 3 | `sql-commandtext-assignment` | `NotesRepository.cs:48` | WARNING | Запрос параметризован; широкий сигнал оставлен для ручного контроля |
| 4 | `sql-commandtext-assignment` | `NotesRepository.cs:62` | WARNING | `Add` параметризован; дефект не подтверждён |
| 5 | `weak-hash-md5` | `Signals.cs:16` | WARNING | Неприменимо к security-свойству в текущем назначении |
| 6 | `password-field-literal` | `Signals.cs:23` | INFO | Ложное срабатывание: имя поля формы |
| 7 | `insecure-random` | `Signals.cs:30` | WARNING | Небезопасная реализация остаётся недостижимой; повторный анализ нужен до подключения метода |

## 5. Решение и изменение

Текст SQL теперь не зависит от содержимого `owner` и `q`:

```csharp
cmd.CommandText = """
    SELECT id, owner, tag, text FROM notes
    WHERE owner = $owner AND text LIKE $pattern;
""";
cmd.Parameters.AddWithValue("$owner", owner);
cmd.Parameters.AddWithValue("$pattern", $"%{q}%");
```

Значения передаются драйверу отдельно от структуры команды. Payload больше не может добавить `OR 1=1` в исполняемый SQL. Широкий сигнал присваивания не подавлен и остаётся точкой ручного контроля.

## 6. Проверка и сравнение

| Свидетельство | До | После | Вывод |
| --- | --- | --- | --- |
| `sql-commandtext-concat` | Присутствует | Отсутствует | Причина сигнала устранена |
| `sql-commandtext-assignment` в `Search` | Конкатенация входных данных | Статический SQL и параметры | Оставшийся широкий сигнал не означает сохранение инъекции |
| Негативный тест | Получена заметка Bob; тест упал | Чужая заметка отсутствует; тест прошёл | Проверяемое свойство восстановлено |
| Штатный поиск | Проходит | Проходит | Исправление не сломало основной сценарий |
| Добавление текста с апострофом | Проходит | Проходит | Регрессионная проверка успешна |
| Общее число Semgrep-сигналов | 8 | 7 | Снижение согласуется с исчезновением узкого сигнала, но не используется как единственное доказательство |

## 7. Ограничения и остаток

Учебные правила Semgrep не доказывают безопасность всего C#-приложения. Проверка охватывает каталог `src`, а негативный тест — конкретный сценарий SQL-инъекции и изоляцию владельцев. Сигнал `insecure-random` нельзя считать безопасным только из-за отсутствия текущего вызова: перед использованием генератора токена потребуется отдельное исправление и тестирование.

## 8. Итог

F-01 закрыт: опасная конкатенация отсутствует, тот же негативный тест проходит, штатные проверки сохранены. Семь оставшихся сигналов не скрыты и разобраны по назначению, достижимости и остаточному риску.
