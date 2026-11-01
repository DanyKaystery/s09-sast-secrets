using System.Security.Cryptography;
using System.Text;

namespace NotesApi;

// Дополнительные места, на которые может сработать статический анализатор.
// Задача семинара - по каждому сигналу самостоятельно определить статус
// (подтвержден / ложное срабатывание / неприменим) и обосновать его через
// применимость и достижимость. Правильные статусы здесь НЕ подписаны сознательно.

public static class Signals
{
    // Вычисление ключа кеша.
    public static string CacheKey(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    // Проверка наличия поля формы по имени.
    public static bool IsPasswordFieldPresent(IDictionary<string, string> form)
    {
        const string passwordFieldName = "password";
        return form.ContainsKey(passwordFieldName);
    }

    // Генерация значения токена доступа.
    public static string NewAccessToken()
    {
        var rng = new Random();
        var sb = new StringBuilder();
        for (var i = 0; i < 16; i++)
            sb.Append((char)('a' + rng.Next(0, 26)));
        return sb.ToString();
    }
}
