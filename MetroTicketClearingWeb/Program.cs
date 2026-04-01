var builder = WebApplication.CreateBuilder(args);

// 添加Razor Pages服务
builder.Services.AddRazorPages();

var app = builder.Build();

// 配置中间件
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 映射Razor Pages路由
app.MapRazorPages();

app.Run();