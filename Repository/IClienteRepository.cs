using PracticaProgramada4_G4.Models;

namespace PracticaProgramada4_G4.Repository
{
    public interface IClienteRepository
    {
        Task<List<Cliente>> ListarAsync(string? nombre = null);
        Task<bool> InsertarAsync(string nombre, int edad);
        Task<bool> ActualizarPorNombreAsync(string nombreFiltro, string? nombreNuevo = null, int? edad = null, string? ciudad = null);
        Task<bool> ActualizarPorIdAsync(string id, string? nombreNuevo = null, int? edad = null, string? ciudad = null);
        Task<bool> EliminarPorNombreAsync(string nombre);
        Task<bool> EliminarPorIdAsync(string id);
    }
}
