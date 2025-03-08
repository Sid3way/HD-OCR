using Emgu.CV.LineDescriptor;
using IHCWargames.Api.Models;
using IHCWargames.Api.Repositories;

namespace IHCWargames.Api.Services;

public class ScoreService
{
    private readonly ComputerVisionService _computerVisionService;
    private readonly ScoreRepository _scoreRepository;
    private readonly ILogger<ScoreService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _imagesPath;

    public ScoreService(ComputerVisionService computerVisionService, ScoreRepository scoreRepository)
    {
        
        // Path to the image you want to process
        _imagesPath = @"../Images/ToScan/";
        _computerVisionService = computerVisionService;
        _scoreRepository = scoreRepository;
        _httpClient = new HttpClient();
    }
    
    public async Task<bool> AddScore(AddScoreCommand command)
    {
        //command.ImageUrl : Read image from here
        var requestId = Guid.NewGuid();
        if (!command.ImageUrl.EndsWith("png"))
        {
            Console.WriteLine("Invalid image url");
            return false;
        }
        var imageBytes = await _httpClient.GetByteArrayAsync(command.ImageUrl);
        var imagePath = $"{_imagesPath}{requestId}.png";
        await File.WriteAllBytesAsync(imagePath, imageBytes);
        var xpAmount = _computerVisionService.GetXpFromImage(imagePath, requestId, true);
        CleanupImages(requestId);
        if (xpAmount == 0)
            return false;
        _scoreRepository.AddScore(xpAmount, command);
        return true;
    }

    private void CleanupImages(Guid requestId)
    {
        var imagePath = $"{_imagesPath}{requestId}.png";
        var croppedImagePath = $"{_imagesPath}{requestId}_cropped.png";
        File.Delete(imagePath);
        File.Delete(croppedImagePath);
    }
    
    public Dictionary<ArmadaEnum, int> GetArmadasScores()
    {
        var res = new Dictionary<ArmadaEnum, int>();
        var fleetScores = _scoreRepository.GetFleetScores();
        var armadaStructure = _scoreRepository.GetArmadasStructure();
        foreach (var keyValuePair in armadaStructure)
        {
            var armadaXp = 0;
            foreach (var fleet in keyValuePair.Value)
            {
                if (fleetScores.TryGetValue(fleet, out var fleetScore))
                    armadaXp += fleetScore;
            }
            res[keyValuePair.Key] = armadaXp;
        }
        return res;
    }

    public Dictionary<FleetEnum, int> GetFleetScores()
    {
        return _scoreRepository.GetFleetScores();
    }
}