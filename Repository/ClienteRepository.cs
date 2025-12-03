using System.Text;
using System.Text.Json;
using PracticaProgramada4_G4.Models;

namespace PracticaProgramada4_G4.Repository
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://paginas-web-cr.com/Api/apis/mongodb.php";

        public ClienteRepository(HttpClient httpClient) => _http = httpClient;

        public async Task<List<Cliente>> ListarAsync(string? nombre = null)
        {
            var url = $"{BaseUrl}?coleccion=clientes";
            if (!string.IsNullOrWhiteSpace(nombre))
                url += $"&nombre={Uri.EscapeDataString(nombre)}";

            var payload = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("documentos", out var docs) || docs.ValueKind != JsonValueKind.Array)
                return new List<Cliente>();

            var lista = new List<Cliente>();

            foreach (var it in docs.EnumerateArray())
            {
                string? id = null;
                if (it.TryGetProperty("_id", out var idObj) && idObj.ValueKind == JsonValueKind.Object)
                {
                    if (idObj.TryGetProperty("$oid", out var oidEl) && oidEl.ValueKind == JsonValueKind.String)
                        id = oidEl.GetString();
                }

                string nombreVal = it.TryGetProperty("nombre", out var nEl) && nEl.ValueKind == JsonValueKind.String
                    ? nEl.GetString() ?? string.Empty
                    : string.Empty;

                int? edadVal = null;
                if (it.TryGetProperty("edad", out var eEl))
                {
                    if (eEl.ValueKind == JsonValueKind.Number && eEl.TryGetInt32(out var eNum))
                        edadVal = eNum;
                    else if (eEl.ValueKind == JsonValueKind.String && int.TryParse(eEl.GetString(), out var eStr))
                        edadVal = eStr;
                }

                string? ciudadVal = it.TryGetProperty("ciudad", out var cEl) && cEl.ValueKind == JsonValueKind.String
                    ? cEl.GetString()
                    : null;

                lista.Add(new Cliente
                {
                    _id = id,
                    Nombre = nombreVal,
                    Edad = edadVal,
                    Ciudad = ciudadVal
                });
            }

            return lista;
        }

        public async Task<bool> InsertarAsync(string nombre, int edad)
        {
            var body = new { coleccion = "clientes", datos = new { nombre, edad } };
            var resp = await _http.PostAsync(BaseUrl, AsJson(body));
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarPorNombreAsync(string nombreFiltro, string? nombreNuevo = null, int? edad = null, string? ciudad = null)
        {
            var body = new
            {
                coleccion = "clientes",
                filtro = new { nombre = nombreFiltro },
                datos = new { nombre = nombreNuevo, edad, ciudad }
            };

            var req = new HttpRequestMessage(HttpMethod.Put, BaseUrl) { Content = AsJson(body) };
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarPorIdAsync(string id, string? nombreNuevo = null, int? edad = null, string? ciudad = null)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 24)
                throw new ArgumentException("El id debe ser un ObjectId de 24 caracteres.", nameof(id));

            var datos = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(nombreNuevo)) datos["nombre"] = nombreNuevo!;
            if (edad.HasValue) datos["edad"] = edad.Value;
            if (!string.IsNullOrWhiteSpace(ciudad)) datos["ciudad"] = ciudad!;
            if (datos.Count == 0)
                throw new InvalidOperationException("No se recibió ningún campo para actualizar.");

            var r1 = await TryUpdateWithFilter(new Dictionary<string, object> { ["_id"] = id }, datos);
            if (r1.changed) return true;

            var correo = await GetCorreoByIdAsync(id);
            if (string.IsNullOrWhiteSpace(correo))
                throw new InvalidOperationException("No fue posible localizar el documento por _id para aplicar el fallback.");

            var r2 = await TryUpdateWithFilter(new Dictionary<string, object> { ["correo"] = correo }, datos);
            if (r2.changed) return true;

            throw new InvalidOperationException($"El API no modificó registros. Respuesta: {r2.rawBody}");
        }

        public async Task<bool> EliminarPorIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 24)
                throw new ArgumentException("El id debe ser un ObjectId de 24 caracteres.", nameof(id));

            var d1 = await TryDeleteWithFilter(new Dictionary<string, object> { ["_id"] = id });
            if (d1.changed) return true;

            var correo = await GetCorreoByIdAsync(id);
            if (string.IsNullOrWhiteSpace(correo))
                throw new InvalidOperationException("No fue posible localizar el documento por _id para aplicar el fallback.");

            var d2 = await TryDeleteWithFilter(new Dictionary<string, object> { ["correo"] = correo });
            if (d2.changed) return true;

            throw new InvalidOperationException($"El API no eliminó registros. Respuesta: {d2.rawBody}");
        }

        public async Task<bool> EliminarPorNombreAsync(string nombre)
        {
            var body = new { coleccion = "clientes", filtro = new { nombre } };
            var req = new HttpRequestMessage(HttpMethod.Delete, BaseUrl) { Content = AsJson(body) };
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        private async Task<string?> GetCorreoByIdAsync(string id)
        {
            var url = $"{BaseUrl}?coleccion=clientes";
            var payload = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("documentos", out var docs) || docs.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var it in docs.EnumerateArray())
            {
                string? oid = null;
                if (it.TryGetProperty("_id", out var idObj) && idObj.ValueKind == JsonValueKind.Object)
                    if (idObj.TryGetProperty("$oid", out var oidEl) && oidEl.ValueKind == JsonValueKind.String)
                        oid = oidEl.GetString();

                if (string.Equals(oid, id, StringComparison.OrdinalIgnoreCase))
                {
                    if (it.TryGetProperty("correo", out var cEl) && cEl.ValueKind == JsonValueKind.String)
                        return cEl.GetString();
                    break;
                }
            }
            return null;
        }

        private async Task<(bool changed, string rawBody)> TryUpdateWithFilter(Dictionary<string, object> filtro, Dictionary<string, object> datos)
        {
            var body = new Dictionary<string, object>
            {
                ["coleccion"] = "clientes",
                ["filtro"] = filtro,
                ["datos"] = datos
            };

            var req = new HttpRequestMessage(HttpMethod.Put, BaseUrl) { Content = AsJson(body) };
            var resp = await _http.SendAsync(req);
            var txt = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode) return (false, txt);

            try
            {
                using var doc = JsonDocument.Parse(txt);
                var root = doc.RootElement;

                int modified = 0;
                if (root.TryGetProperty("modifiedCount", out var mc) && mc.TryGetInt32(out var v1)) modified = v1;
                else if (root.TryGetProperty("nModified", out var nm) && nm.TryGetInt32(out var v2)) modified = v2;
                else if (root.TryGetProperty("modified", out var md) && md.TryGetInt32(out var v3)) modified = v3;
                else if (root.TryGetProperty("modificados", out var mx) && mx.TryGetInt32(out var v4)) modified = v4;

                if (modified > 0) return (true, txt);

                int matched = 0;
                bool acknowledged = false;

                if (root.TryGetProperty("matchedCount", out var mt) && mt.TryGetInt32(out var mv)) matched = mv;
                if (root.TryGetProperty("acknowledged", out var ack) && ack.ValueKind == JsonValueKind.True) acknowledged = true;

                if (acknowledged && matched > 0) return (true, txt);

                return (false, txt);
            }
            catch
            {
                return (false, txt);
            }
        }

        private async Task<(bool changed, string rawBody)> TryDeleteWithFilter(Dictionary<string, object> filtro)
        {
            var body = new Dictionary<string, object>
            {
                ["coleccion"] = "clientes",
                ["filtro"] = filtro
            };

            var req = new HttpRequestMessage(HttpMethod.Delete, BaseUrl) { Content = AsJson(body) };
            var resp = await _http.SendAsync(req);
            var txt = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode) return (false, txt);

            try
            {
                using var doc = JsonDocument.Parse(txt);
                var root = doc.RootElement;

                int deleted = 0;
                if (root.TryGetProperty("deletedCount", out var dc) && dc.TryGetInt32(out var v1)) deleted = v1;
                else if (root.TryGetProperty("deleted", out var dd) && dd.TryGetInt32(out var v2)) deleted = v2;
                else if (root.TryGetProperty("n", out var n) && n.TryGetInt32(out var v3)) deleted = v3;
                else if (root.TryGetProperty("eliminados", out var ex) && ex.TryGetInt32(out var v4)) deleted = v4;

                return (deleted > 0, txt);
            }
            catch
            {
                return (false, txt);
            }
        }

        private static StringContent AsJson(object obj)
        {
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
#if NET8_0_OR_GREATER
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
#else
                IgnoreNullValues = true
#endif
            });
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}
