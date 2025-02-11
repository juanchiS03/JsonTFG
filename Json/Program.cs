var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configurar la canalizaci�n de solicitudes HTTP.
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=OwlToJson}/{action=Index}/{id?}");

app.Run();
