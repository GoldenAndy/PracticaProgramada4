using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using PracticaProgramada4_G4.Models;
using PracticaProgramada4_G4.Business;

namespace PracticaProgramada4_G4.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IClienteService _clientes;
        private readonly IWebHostEnvironment _env;

        public HomeController(
            ILogger<HomeController> logger,
            IClienteService clientes,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _clientes = clientes;
            _env = env;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ============= PARTIALS =============
        [HttpGet]
        public async Task<IActionResult> ComponenteListado(string? nombre)
        {
            try
            {
                var data = await _clientes.ListarAsync(nombre);
                return PartialView("_ComponenteListado", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo listado de clientes.");
                var msg = _env.IsDevelopment()
                    ? $"No fue posible cargar el listado de clientes. Detalle: {ex.Message}"
                    : "No fue posible cargar el listado de clientes.";
                return PartialView("_ErrorMini", msg);
            }
        }

        [HttpGet]
        public IActionResult ComponenteInsertar() => PartialView("_ComponenteInsertar");

        [HttpGet]
        public IActionResult ComponenteActualizarEliminar() => PartialView("_ComponenteActualizarEliminar");

        // Herramienta de diagnóstico del API (opcional)
        [HttpGet]
        public async Task<IActionResult> PingApi(string? nombre)
        {
            var baseUrl = "https://paginas-web-cr.com/Api/apis/mongodb.php";
            var url = $"{baseUrl}?coleccion=clientes";
            if (!string.IsNullOrWhiteSpace(nombre))
                url += $"&nombre={Uri.EscapeDataString(nombre)}";

            using var http = new HttpClient();
            var resp = await http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            return Content($"STATUS {(int)resp.StatusCode}\n{body}", "text/plain");
        }

        // ============= ACCIONES AJAX =============
        [HttpPost]
        public async Task<IActionResult> InsertarCliente([FromForm] string nombre, [FromForm] int edad)
        {
            try
            {
                await _clientes.RegistrarAsync(nombre, edad);
                return Json(new { ok = true, msg = "Cliente registrado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo al insertar cliente.");
                return BadRequest(new { ok = false, msg = ex.Message });
            }
        }

        // SOLO por ID
        [HttpPost]
        public async Task<IActionResult> ActualizarPorId(
            [FromForm] string id,
            [FromForm] string? nombreNuevo,
            [FromForm] int? edad,
            [FromForm] string? ciudad)
        {
            try
            {
                if (!IsValidObjectId(id))
                    return BadRequest(new { ok = false, msg = "El id no tiene formato válido de ObjectId (24 hex)." });

                // Al menos un campo a modificar
                if (string.IsNullOrWhiteSpace(nombreNuevo) && !edad.HasValue && string.IsNullOrWhiteSpace(ciudad))
                    return BadRequest(new { ok = false, msg = "Indica al menos un campo a actualizar." });

                await _clientes.ActualizarPorIdAsync(id, nombreNuevo, edad, ciudad);
                return Json(new { ok = true, msg = "Cliente actualizado por id." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo al actualizar por id.");
                return BadRequest(new { ok = false, msg = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EliminarPorId([FromForm] string id)
        {
            try
            {
                if (!IsValidObjectId(id))
                    return BadRequest(new { ok = false, msg = "El id no tiene formato válido de ObjectId (24 hex)." });

                await _clientes.EliminarPorIdAsync(id);
                return Json(new { ok = true, msg = "Cliente eliminado por id." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo al eliminar por id.");
                return BadRequest(new { ok = false, msg = ex.Message });
            }
        }

        // ============= Helpers =============
        private static bool IsValidObjectId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 24) return false;
            // 24 caracteres hexadecimales
            return Regex.IsMatch(id, "^[0-9a-fA-F]{24}$");
        }
    }
}
