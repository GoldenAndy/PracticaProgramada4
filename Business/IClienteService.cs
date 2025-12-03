using PracticaProgramada4_G4.Models;

namespace PracticaProgramada4_G4.Business
{
    public interface IClienteService
    {
        Task<List<Cliente>> ListarAsync(string? nombre = null);
        Task RegistrarAsync(string nombre, int edad);
        Task ActualizarPorNombreAsync(string nombreFiltro, string? nombreNuevo = null, int? edad = null, string? ciudad = null);
        Task ActualizarPorIdAsync(string id, string? nombreNuevo = null, int? edad = null, string? ciudad = null);
        Task EliminarPorNombreAsync(string nombre);
        Task EliminarPorIdAsync(string id);
    }
}
