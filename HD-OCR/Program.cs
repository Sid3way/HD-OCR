// See https://aka.ms/new-console-template for more information

// Path to the image you want to process

using IHCWargames.Api.Services;

var imagesPath = @"../../../../Images/ToScan/";


var files = Directory.GetFiles(imagesPath).Where(f => f.EndsWith(".png"));
var totalXp = 0;
var CVService = new ComputerVisionService();

foreach (var imagePath in files)
{
    var xpAmmount = CVService.GetXpFromImage(imagePath, true);
    Console.WriteLine("Found XP: " + xpAmmount);
    totalXp += xpAmmount;
}

Console.WriteLine($"Total XP: {totalXp}");
