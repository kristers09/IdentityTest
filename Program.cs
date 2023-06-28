using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using System.Data.SqlClient;
using IdentityTest.Data.Identity;
using Microsoft.AspNetCore.Identity;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using IdentityTest.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddDefaultTokenProviders();

builder.Services.AddTransient<IUserStore<IdentityUser>, DapperUserStore>();
builder.Services.AddTransient<IRoleStore<IdentityRole>, DapperRoleStore>();

builder.Services.AddTransient<IDbConnection>(
    sp => new SqlConnection(builder.Configuration.GetConnectionString("KristeraLocal"))
);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
