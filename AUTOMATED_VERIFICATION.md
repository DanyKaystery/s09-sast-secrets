# Автоматизированная проверочная цепочка

## AVC-01. Изоляция заметок при поиске

**Проверяемое свойство:** пользователь не должен получать заметки другого владельца через параметр поискового запроса.

1. **Объект и версия до исправления.** Commit `ebb15a1b4bfecefbf2b3580e54b66d62da50e763`; состояние и SHA-256 исходников записаны в `source-before.json`.
2. **Конфигурация запуска.** Semgrep CE 1.175.0 с локальным `semgrep.yml`; тесты .NET SDK 10.0.401. Запуск выполнен GitHub Actions workflow `s09-evidence.yml`.
3. **Исходный finding.** Semgrep отметил присваивание и конкатенацию SQL в `NotesRepository.Search` — `sql-commandtext-assignment` и `sql-commandtext-concat` на одной причине.
4. **Интерпретация.** Параметры `owner` и `q` попадали в SQL как часть текста команды. Вход `%' OR 1=1 -- ` делал условие истинным для чужих строк.
5. **Наблюдаемое проявление.** Включённый тест `Search_Injection_Does_Not_Return_Bobs_Note` не прошёл: поиск от Alice вернул также заметку Bob. Итого: 4 пройдено, 1 не пройден.
6. **Изменение.** В commit `f1ee86edfe55b56af7cca29089469b003fd943aa` SQL стал статическим, а значения передаются параметрами `$owner` и `$pattern`.
7. **Регрессия и повторная проверка.** На версии `f6f3475fafcee6ce8e39dc262c1c8d5b638131c8` прошли все 5 тестов. Повторный Semgrep не содержит `sql-commandtext-concat`; число сигналов уменьшилось с 8 до 7.
8. **Решение по остаточным сигналам.** Оставшийся `sql-commandtext-assignment` не подавлен: широкое правило продолжает отмечать безопасное параметризованное присваивание для ручного просмотра.

Свидетельства: [отчёт до](reports/actual/run-42628309579/semgrep-before.json), [падение теста](reports/actual/run-42628309579/tests-before.txt), [отчёт после](reports/actual/run-42628309579/semgrep-after.json), [успешные тесты](reports/actual/run-42628309579/tests-after.txt), [манифест до](reports/actual/run-42628309579/source-before.json), [манифест после](reports/actual/run-42628309579/source-after.json).
