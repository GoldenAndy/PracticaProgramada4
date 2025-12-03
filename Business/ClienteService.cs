using PracticaProgramada4_G4.Repository;

namespace PracticaProgramada4_G4.Business
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _repo;
        public ClienteService(IClienteRepository repo) => _repo = repo;

        public Task<List<PracticaProgramada4_G4.Models.Cliente>> ListarAsync(string? nombre = null)
            => _repo.ListarAsync(nombre?.Trim());

        public async Task RegistrarAsync(string nombre, int edad)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.");
            if (edad < 0) throw new ArgumentException("La edad no puede ser negativa.");

            if (!await _repo.InsertarAsync(nombre.Trim(), edad))
                throw new InvalidOperationException("No fue posible insertar el cliente.");
        }

        public async Task ActualizarPorNombreAsync(string nombreFiltro, string? nombreNuevo = null, int? edad = null, string? ciudad = null)
        {
            if (string.IsNullOrWhiteSpace(nombreFiltro)) throw new ArgumentException("Debe indicar el nombre a filtrar.");
            if (!await _repo.ActualizarPorNombreAsync(nombreFiltro.Trim(), nombreNuevo?.Trim(), edad, ciudad?.Trim()))
                throw new InvalidOperationException("No fue posible actualizar el cliente.");
        }

        public async Task ActualizarPorIdAsync(string id, string? nombreNuevo, int? edad, string? ciudad)
        {
            var ok = await _repo.ActualizarPorIdAsync(id, nombreNuevo, edad, ciudad);
            if (!ok) throw new InvalidOperationException("El API no confirmó la actualización.");
        }

        public async Task EliminarPorNombreAsync(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Debe indicar el nombre.");
            if (!await _repo.EliminarPorNombreAsync(nombre.Trim()))
                throw new InvalidOperationException("No fue posible eliminar el cliente.");
        }

        public async Task EliminarPorIdAsync(string id)
        {
            var ok = await _repo.EliminarPorIdAsync(id);
            if (!ok) throw new InvalidOperationException("El API no confirmó la eliminación.");
        }
    }
}
