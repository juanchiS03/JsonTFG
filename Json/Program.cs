var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configurar la canalización de solicitudes HTTP.
app.UseStaticFiles(); // Agrega esta línea para servir archivos estáticos

app.UseRouting();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=OwlToJson}/{action=Index}/{id?}");

app.Run();
