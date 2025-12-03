// wwwroot/js/clientes.js
(() => {
    const ZONA_LISTADO = document.getElementById("zonaListado");
    const ZONA_INSERTAR = document.getElementById("zonaInsertar");
    const ZONA_ACT_ELIM = document.getElementById("zonaActualizarEliminar");
    const ALERT_GLOBAL = document.getElementById("alertGlobal");

    // ---------- UI ----------
    function setAlert(type, text) {
        if (!ALERT_GLOBAL) return;
        ALERT_GLOBAL.className = `alert alert-${type}`;
        ALERT_GLOBAL.textContent = text;
        ALERT_GLOBAL.classList.remove("d-none");
        clearTimeout(setAlert._t);
        setAlert._t = setTimeout(() => ALERT_GLOBAL.classList.add("d-none"), 3500);
    }

    function htmlLoading(msg) {
        return `
      <div class="text-center py-4">
        <div class="spinner-border" role="status" aria-hidden="true"></div>
        <div class="mt-2">${msg || "Cargando…"}</div>
      </div>`;
    }

    async function loadPartial(targetEl, url, loadingMsg) {
        if (!targetEl) return;
        try {
            targetEl.innerHTML = htmlLoading(loadingMsg);
            const res = await fetch(url, { method: "GET", headers: { "X-Requested-With": "Fetch" } });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            targetEl.innerHTML = await res.text();
        } catch (err) {
            targetEl.innerHTML = `
        <div class="alert alert-danger mb-0">
          No fue posible cargar el componente. ${err instanceof Error ? err.message : ""}
        </div>`;
        }
    }

    async function postForm(endpoint, formDataObj) {
        const body = new URLSearchParams();
        Object.entries(formDataObj).forEach(([k, v]) => {
            if (v !== undefined && v !== null) body.append(k, String(v));
        });

        console.log("[postForm] →", endpoint, Object.fromEntries(body));

        const res = await fetch(endpoint, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                "X-Requested-With": "Fetch"
            },
            body
        });

        let data;
        try { data = await res.json(); }
        catch { data = { ok: res.ok, msg: res.ok ? "OK" : `HTTP ${res.status}` }; }

        console.log("[postForm] ←", endpoint, res.status, data);

        if (!res.ok || data?.ok === false) {
            throw new Error(data?.msg || `Error HTTP ${res.status}`);
        }
        return data;
    }

    function getFiltroNombreActual() {
        const form = ZONA_LISTADO?.querySelector("#formFiltroListado");
        if (!form) return null;
        const v = (new FormData(form).get("nombre") || "").toString().trim();
        return v || null;
    }

    async function refrescarListadoActual() {
        const nombre = getFiltroNombreActual();
        const qs = nombre ? `?nombre=${encodeURIComponent(nombre)}` : "";
        await loadPartial(ZONA_LISTADO, `/Home/ComponenteListado${qs}`, "Actualizando listado…");
    }

    // ---------- Carga inicial (secuencial para poder cablear listeners) ----------
    async function autoload() {
        await loadPartial(ZONA_LISTADO, "/Home/ComponenteListado", "Cargando listado…");
        await loadPartial(ZONA_INSERTAR, "/Home/ComponenteInsertar", "Cargando formulario…");
        await loadPartial(ZONA_ACT_ELIM, "/Home/ComponenteActualizarEliminar", "Cargando formulario…");

        wireListado();
        wireInsertar();
        wireActualizarEliminar(); // <— aquí bindo a #formActualizarId y #formEliminarId
    }

    // ---------- Listado ----------
    function wireListado() {
        // Filtro
        const formFiltro = ZONA_LISTADO?.querySelector("#formFiltroListado");
        formFiltro?.addEventListener("submit", async (ev) => {
            ev.preventDefault();
            const nombre = (new FormData(formFiltro).get("nombre") || "").toString().trim();
            const qs = nombre ? `?nombre=${encodeURIComponent(nombre)}` : "";
            await loadPartial(ZONA_LISTADO, `/Home/ComponenteListado${qs}`, "Filtrando…");
            wireListado(); // re-cableo por si el partial se volvió a renderizar
            wireIdCopy();  // y el click de ID de nuevo
        });

        wireIdCopy();
    }

    // Click en ID para autocompletar formularios por ID
    function wireIdCopy() {
        ZONA_LISTADO?.querySelectorAll(".js-copiar-id").forEach(btn => {
            btn.addEventListener("click", () => {
                const id = btn.getAttribute("data-id") || "";
                const fUpd = ZONA_ACT_ELIM?.querySelector("#formActualizarId input[name=id]");
                const fDel = ZONA_ACT_ELIM?.querySelector("#formEliminarId input[name=id]");
                if (fUpd) fUpd.value = id;
                if (fDel) fDel.value = id;
                navigator.clipboard?.writeText(id).catch(() => { });
                setAlert("info", "ID copiado y autocompletado.");
            });
        });
    }

    // ---------- Insertar ----------
    function wireInsertar() {
        const form = ZONA_INSERTAR?.querySelector("#formInsertar");
        form?.addEventListener("submit", async (ev) => {
            ev.preventDefault();
            const btn = form.querySelector("[type=submit]");
            try {
                btn && (btn.disabled = true);
                const fd = new FormData(form);
                const nombre = (fd.get("nombre") || "").toString().trim();
                const edad = Number(fd.get("edad"));
                if (!nombre) throw new Error("El nombre es obligatorio.");
                if (!Number.isFinite(edad) || edad < 0) throw new Error("Edad inválida.");
                await postForm("/Home/InsertarCliente", { nombre, edad });
                setAlert("success", "Cliente registrado correctamente.");
                form.reset();
                await refrescarListadoActual();
            } catch (e) {
                console.error(e);
                setAlert("danger", e instanceof Error ? e.message : "Error al insertar.");
            } finally {
                btn && (btn.disabled = false);
            }
        });
    }

    // ---------- Actualizar / Eliminar por ID ----------
    function wireActualizarEliminar() {
        const formUpd = ZONA_ACT_ELIM?.querySelector("#formActualizarId");
        const formDel = ZONA_ACT_ELIM?.querySelector("#formEliminarId");

        formUpd?.addEventListener("submit", async (ev) => {
            ev.preventDefault();
            const btn = formUpd.querySelector("[type=submit]");
            try {
                btn && (btn.disabled = true);
                const fd = new FormData(formUpd);
                const id = (fd.get("id") || "").toString().trim();
                const nombreNuevo = (fd.get("nombreNuevo") || "").toString().trim() || null;
                const edadStr = (fd.get("edad") || "").toString().trim();
                const ciudad = (fd.get("ciudad") || "").toString().trim() || null;
                const edad = edadStr ? Number(edadStr) : null;

                console.log("[ActualizarPorId] payload", { id, nombreNuevo, edad, ciudad });

                if (!id) throw new Error("Debes indicar el id.");
                if (edad !== null && (!Number.isFinite(edad) || edad < 0)) throw new Error("Edad inválida.");

                await postForm("/Home/ActualizarPorId", { id, nombreNuevo, edad, ciudad });
                setAlert("success", "Cliente actualizado por id.");
                formUpd.reset();
                await refrescarListadoActual();
            } catch (e) {
                console.error(e);
                setAlert("danger", e instanceof Error ? e.message : "Ocurrió un error en la operación.");
            } finally {
                btn && (btn.disabled = false);
            }
        });

        formDel?.addEventListener("submit", async (ev) => {
            ev.preventDefault();
            const btn = formDel.querySelector("[type=submit]");
            try {
                btn && (btn.disabled = true);
                const fd = new FormData(formDel);
                const id = (fd.get("id") || "").toString().trim();

                console.log("[EliminarPorId] payload", { id });

                if (!id) throw new Error("Debes indicar el id.");
                await postForm("/Home/EliminarPorId", { id });
                setAlert("success", "Cliente eliminado por id.");
                formDel.reset();
                await refrescarListadoActual();
            } catch (e) {
                console.error(e);
                setAlert("danger", e instanceof Error ? e.message : "Ocurrió un error en la operación.");
            } finally {
                btn && (btn.disabled = false);
            }
        });
    }

    document.addEventListener("DOMContentLoaded", autoload);
})();
