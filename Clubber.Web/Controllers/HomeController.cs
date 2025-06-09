using Clubber.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Clubber.Web.Controllers;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes",
	Justification = "Used by ASP.NET Core MVC routing")]
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public sealed class HomeController : Controller
{
	public IActionResult Index()
	{
		return View();
	}

	public IActionResult Privacy()
	{
		return View();
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
