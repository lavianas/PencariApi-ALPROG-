using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PencariApi.Services;

public static class GeminiChatService
{
    private const string ApiKey = "AQ.Ab8RN6JFth9V-QE3cg5L7PV6DMKdlGqJ5y1fi7-LnXIq3ARpaQ";
    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key=";

    private static readonly HttpClient HttpClient = new HttpClient();

    public static async Task<string> AskAsync(string userMessage, string role)
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey.Contains("ISI_API_KEY"))
        {
            return "API key Gemini belum diisi. Buka file Services/GeminiChatService.cs lalu isi ApiKey terlebih dahulu.";
        }

        try
        {
            string systemPrompt = GetSystemPrompt(role);

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = systemPrompt }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userMessage }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(requestBody);

            string fullUrl = $"{ApiUrl}{ApiKey}";
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await HttpClient.SendAsync(request);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Gagal menghubungi Gemini API.\n\nStatus: {response.StatusCode}\n\nDetail:\n{responseJson}";
            }

            string answer = ExtractTextFromResponse(responseJson);

            if (string.IsNullOrWhiteSpace(answer))
            {
                return $"AI merespons, tapi teks jawaban tidak terbaca.\n\nRaw response:\n{responseJson}";
            }

            return answer;
        }
        catch (Exception ex)
        {
            return $"Terjadi error saat menghubungi AI:\n{ex.Message}";
        }
    }

    private static string GetSystemPrompt(string role)
    {
        return
            $"Kamu adalah PencariApi OPTIMA AI Assistant untuk aplikasi Optimasi Rute Pemadam Api via Kamera IoT dan Algoritma Genetika. " +
            $"Role pengguna saat ini adalah {role}. " +
            $"Jawab dalam bahasa Indonesia yang jelas, singkat, profesional, dan mudah dipahami. " +
            $"Konteks aplikasi: sistem memiliki fitur login admin/user, verifikasi wajah, dashboard admin, dashboard user, laporan lokasi Surabaya, Google Maps, database laporan, pos pemadam Surabaya, kamera IoT, dan algoritma genetika untuk optimasi rute. " +
            $"Jika user adalah Admin, bantu menjelaskan laporan klien, data insiden, pos pemadam, rute optimal, kamera IoT, dan analitik. " +
            $"Jika user adalah User, bantu menjelaskan cara melapor lokasi, membuka maps, melihat pos damkar terdekat, panduan evakuasi, dan status laporan. " +
            $"Jika ditanya hal darurat, sarankan tetap tenang, jauhi area bahaya, hubungi layanan darurat 112, dan ikuti arahan petugas.";
    }

    private static string ExtractTextFromResponse(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("candidates", out JsonElement candidatesArray) &&
                candidatesArray.ValueKind == JsonValueKind.Array &&
                candidatesArray.GetArrayLength() > 0)
            {
                JsonElement firstCandidate = candidatesArray[0];
                if (firstCandidate.TryGetProperty("content", out JsonElement contentObject))
                {
                    if (contentObject.TryGetProperty("parts", out JsonElement partsArray) &&
                        partsArray.ValueKind == JsonValueKind.Array &&
                        partsArray.GetArrayLength() > 0)
                    {
                        JsonElement firstPart = partsArray[0];
                        if (firstPart.TryGetProperty("text", out JsonElement textElement))
                        {
                            return textElement.GetString()?.Trim() ?? "";
                        }
                    }
                }
            }

            return "";
        }
        catch
        {
            return "";
        }
    }
}
