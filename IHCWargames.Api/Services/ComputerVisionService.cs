using Tesseract;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Util;

namespace IHCWargames.Api.Services;

public class ComputerVisionService
{
    private readonly string _templatePath;
    private readonly string _tessDataPath;
    private readonly Image<Gray, byte> _grayIcon;

    public ComputerVisionService()
    {
        // Path to the image you want to process
        _templatePath = @"../Images/Templates/XP_TIGHT.png";

        // Path to the tessdata folder where language files are stored
        _tessDataPath = @"../LLMData/";
        
        var tmp = Path.GetFullPath(_templatePath);
        var tmp2 = Path.GetFullPath(".");
        
        Image<Bgr, byte> iconImage = new(_templatePath);
        _grayIcon = iconImage.Convert<Gray, byte>();

    }
    
    public int GetXpFromImage(string imageFilePath, Guid operationUniqueId, bool drawDebug = false)
    {
        CreateCroppedImage(imageFilePath, operationUniqueId, drawDebug);
        return ReadXPFromCroppedImage(GenerateCroppedPathFromImagePath(imageFilePath));
    }

    private static string GenerateCroppedPathFromImagePath(string imageFilePath)
    {
        return imageFilePath.Replace(".png", "_cropped.png");
    }
    
    private int ReadXPFromCroppedImage(string imageFilePath)
    {
        using (var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default))
        {
            // Load the image from the file
            using (var img = Pix.LoadFromFile(imageFilePath))
            {
                // Perform OCR on the image and get the result
                using (var page = engine.Process(img))
                {
                    // Output the recognized text
                    Console.WriteLine("Recognized Text: ");
                    var fullText = page.GetText();
                    Console.WriteLine(fullText);
                    var xpAmountString = fullText.Split("/").FirstOrDefault() ?? fullText;
                    return int.TryParse(xpAmountString.Replace(",", ""), out int xpAmount) ? xpAmount : 0;
                }
            }
        }
    }

    private void CreateCroppedImage(string sourceImagePath, Guid croppedImageId, bool drawDebug)
    {
        // Load the source image to memory
        Image<Bgr, byte> mainImage = new(sourceImagePath);

        // Convert image to grayscale
        Image<Gray, byte> grayMain = mainImage.Convert<Gray, byte>();

        // Create the SIFT detector and descriptor
        var sift = new SIFT();

        // Detect keypoints and compute descriptors for the icon (template)
        var keypointsIcon = new VectorOfKeyPoint();
        var descriptorsIcon = new Mat();
        sift.DetectAndCompute(_grayIcon, null, keypointsIcon, descriptorsIcon, false);

        // Detect keypoints and compute descriptors for the main image
        var keypointsMain = new VectorOfKeyPoint();
        var descriptorsMain = new Mat();
        sift.DetectAndCompute(grayMain, null, keypointsMain, descriptorsMain, false);

        // FLANN matcher expects descriptors to be of type CV_32F
        if (descriptorsIcon.Depth != Emgu.CV.CvEnum.DepthType.Cv32F)
        {
            descriptorsIcon.ConvertTo(descriptorsIcon, Emgu.CV.CvEnum.DepthType.Cv32F);
        }

        if (descriptorsMain.Depth != Emgu.CV.CvEnum.DepthType.Cv32F)
        {
            descriptorsMain.ConvertTo(descriptorsMain, Emgu.CV.CvEnum.DepthType.Cv32F);
        }

        // Use a FLANN-based matcher with appropriate parameters
        var matcher = new FlannBasedMatcher(new KdTreeIndexParams(5), new SearchParams(50));
        matcher.Add(descriptorsIcon);

        // Perform KNN matching (with k = 2)
        VectorOfVectorOfDMatch knnMatches = new VectorOfVectorOfDMatch();
        matcher.KnnMatch(descriptorsMain, knnMatches, k: 2, null);

        // Filter matches using Lowe's ratio test
        List<MDMatch> goodMatches = [];
        for (var i = 0; i < knnMatches.Size; i++)
        {
            if (knnMatches[i].Size >= 2)
            {
                MDMatch[] matchPair = knnMatches[i].ToArray();
                if (matchPair[0].Distance < 0.75 * matchPair[1].Distance)
                {
                    goodMatches.Add(matchPair[0]);
                }
            }
        }

        // Proceed if we have enough good matches to compute a homography
        if (goodMatches.Count >= 4)
        {
            // Prepare lists of matched points from the icon and the main image.
            PointF[] iconPoints = new PointF[goodMatches.Count];
            PointF[] mainPoints = new PointF[goodMatches.Count];

            // Note: For each match, the train index refers to the icon (template)
            // and the query index refers to the main image.
            for (var i = 0; i < goodMatches.Count; i++)
            {
                iconPoints[i] = keypointsIcon[goodMatches[i].TrainIdx].Point;
                mainPoints[i] = keypointsMain[goodMatches[i].QueryIdx].Point;
            }

            // Compute the homography using RANSAC to filter out outliers.
            Mat homography = CvInvoke.FindHomography(iconPoints, mainPoints, RobustEstimationAlgorithm.Ransac, 3);

            if (!homography.IsEmpty)
            {
                // Define the corners of the icon (template)
                PointF[] iconCorners =
                [
                    new(0, 0),
                    new(_grayIcon.Width, 0),
                    new(_grayIcon.Width, _grayIcon.Height),
                    new(0, _grayIcon.Height)
                ];

                // Transform the icon corners to the main image perspective
                PointF[] transformedCorners = CvInvoke.PerspectiveTransform(iconCorners, homography);

                var minX = float.MaxValue;
                var minY = float.MaxValue;
                var maxX = float.MinValue;
                var maxY = float.MinValue;

                foreach (var pt in transformedCorners)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y > maxY) maxY = pt.Y;
                }

                // Compute the bounding rectangle for the icon.
                Rectangle iconRect = new Rectangle(Point.Round(new PointF(minX, minY)),
                    new Size((int)(maxX - minX), (int)(maxY - minY)));

                // Define a new rectangle that starts at the right edge of the icon.
                var newRectWidth = (int)(iconRect.Width * 2.7);
                var newRectHeight = (int)(iconRect.Height * 1.5);

                // The new rectangle's top-left corner is at (iconRect.Right, iconRect.Y)
                Rectangle rightSideRect = new Rectangle(iconRect.Right, iconRect.Y, newRectWidth, newRectHeight);

                // Crop the ROI from the big image.
                Image<Bgr, byte> croppedImage = mainImage.Copy(rightSideRect);

                // Save the cropped image to a new file
                croppedImage.Save(GenerateCroppedPathFromImagePath(sourceImagePath));


                if (drawDebug)
                {
                    // Draw both rectangles on the image for visualization.
                    CvInvoke.Rectangle(mainImage, iconRect, new Bgr(Color.Red).MCvScalar,
                        2); // Detected icon bounding box
                    CvInvoke.Rectangle(mainImage, rightSideRect, new Bgr(Color.Blue).MCvScalar,
                        2); // Rectangle starting at the right side

                    // Display the result
                    CvInvoke.Imshow("Detected Icon", mainImage);
                    CvInvoke.WaitKey(0);
                    CvInvoke.DestroyAllWindows();
                }
            }
            else
            {
                Console.WriteLine("Homography computation failed.");
            }
        }
        else
        {
            Console.WriteLine("Not enough good matches were found to reliably detect the icon.");
        }
    }
}