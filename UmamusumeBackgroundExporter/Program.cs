using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SQLitePCL;
using System;
using System.Diagnostics;
using System.Text.Json;
using Uma;

public class Config
{
    public static string GetPersistentPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Mengganti 'Local' menjadi 'LocalLow' dan menggabungkan sisanya
        string baseLow = Path.Combine(Path.GetDirectoryName(localAppData), "LocalLow");
        string finalPath = Path.Combine(baseLow, "Cygames", "umamusume");

        return finalPath;
    }

    public string persistentPath { get; set; } = "";
    public string DBKey { get; set; } = "6D5B65336336632554712D73505363386D34377B356370233734532973433633";
    public string DBBaseKey { get; set; } = "F170CEA4DFCEA3E1A5D8C70BD1000000";
    public string ABKey { get; set; } = "532B4631E4A7B9473E7CFB";
    public string GlobalDBKey { get; set; } = "56636B634272377665704162";
    public bool GlobalDB { get; set; } = true;

    public string exportPath { get; set; } = "bg/";
    public string exportExtension { get; set; } = ".png";
    public int quality { get; set; } = 90;
    public int takeMax { get; set; } = -1;
    public bool overwrite { get; set; } = false;
}

public class App
{
    static AssetsManager manager = new AssetsManager();
    static Stopwatch stopwatch;

    public static void ExportTexture(
        UmaAssetBundleStream abStream,
        ImageExportType exportType = ImageExportType.Png,
        string name="output.png", int quality = 90)
    {
        BundleFileInstance bunInst = manager.LoadBundleFile(abStream);

        // 2. Load the assets file from the bundle (assuming index 0)  
        AssetsFileInstance assetsInst = manager.LoadAssetsFileFromBundle(bunInst, 0);

        // 3. Find the Texture2D asset (e.g., by name)  
        AssetFileInfo texInfo = assetsInst.file.GetAssetsOfType(AssetClassID.Texture2D)[0];
        AssetTypeValueField baseField = manager.GetBaseField(assetsInst, texInfo);

        // 4. Parse the texture and resolve its data (including .resS)  
        TextureFile tf = TextureFile.ReadTextureFile(baseField);
        byte[] rawData = tf.FillPictureData(assetsInst);

        // 5. Export to PNG  
        FileStream outStream = File.Create(name);
        tf.DecodeTextureImage(rawData, outStream, exportType, quality);
        manager.UnloadAssetsFile(assetsInst);
        manager.UnloadBundleFile(bunInst);
        abStream.Close();
        outStream.Close();
    }

    static void Main(string[] args)
    {
        Config config;
        try
        {
            Console.WriteLine("Loaded config.json");
            string jsonString = File.ReadAllText("config.json");
            config = JsonSerializer.Deserialize<Config>(jsonString);
        } catch (Exception e)
        {
            Console.WriteLine($"ERROR Loading config.json: {e}\n\nINFO: Defaulting config to hardcoded values.");
            config = new();
            config.persistentPath = Config.GetPersistentPath();
            Console.WriteLine($"INFO: Using {config.persistentPath} as default persistent path.");
        }

        try
        {
            if (config.persistentPath == null)
            {
                config.persistentPath = Config.GetPersistentPath();
                Console.WriteLine($"INFO: persistentPath is null! Defaulting to {config.persistentPath}");
            }
            UmaDatabase.PersistentPath = config.persistentPath;
            UmaDatabase.DBKey = config.GlobalDB ? config.GlobalDBKey : config.DBKey; if (config.GlobalDB) Console.WriteLine("INFO: Using global database.");
            UmaDatabase.DBBaseKey = config.DBBaseKey;
            UmaDatabase.ABKey = config.ABKey;

            UmaDatabase.CreateConnection();
            UmaDatabase.Initialize();
        } catch (Exception e)
        {
            Console.WriteLine($"ERROR Loading database: {e}\n\nAre you sure using the correct keys and are not using the global Umamusume? You can set this settings in config.json");
            Environment.Exit(1);
        }

        var bgList = UmaDatabase.MetaData.Where(x => x.Value.Type == UmaFileType.bg);
        ImageExportType exportType;

        config.exportExtension = config.exportExtension.ToLower();

        if (config.exportExtension == ".png")//png jpg tga bmp
        {
            exportType = ImageExportType.Png;
        } else if (config.exportExtension == ".jpg")
        {
            exportType = ImageExportType.Jpg;
        } else if (config.exportExtension == ".tga")
        {
            exportType = ImageExportType.Tga;
        } else if (config.exportExtension == ".bmp")
        {
            exportType = ImageExportType.Bmp;
        } else
        {
            Console.WriteLine($"WARNING: Unrecognized extension: {config.exportExtension}. Defaulting to .png");
            config.exportExtension = ".png";
            exportType = ImageExportType.Png;
        }

        Console.WriteLine($"INFO: Total backgrounds: {bgList.Count()}");

        if (config.takeMax != -1)
        {
            Console.WriteLine($"INFO: Taking only {config.takeMax} out of {bgList.Count()}");
            bgList = bgList.Take(config.takeMax);
        }

        Console.WriteLine($"INFO: Using quality: {config.quality}");

        int successCount = 0;
        int iterations = 0;
        stopwatch = Stopwatch.StartNew();

        Console.CancelKeyPress += (sender, e) =>
        {
            stopwatch.Stop();
            Console.WriteLine($"\nINFO: Terminated by user\nINFO: Exported {successCount} out of {bgList.Count()}\nExecution time: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedMilliseconds / 1000} s)");

            // Set to true to keep the application alive (manual exit)
            // Set to false (default) to let the OS kill the process after this handler
            e.Cancel = true;

            Environment.Exit(0);
        };

        foreach (var bg in bgList)
        {
            string path = Path.Join(config.exportPath, bg.Key.Replace("bg/", "").Replace("/", "_") + config.exportExtension);
            string abPath = UmaDatabase.ResolvePath(bg.Key);
            if (abPath == null) continue;

            try
            {
                Console.Write($"{iterations} INFO: Loading {bg.Key}... ");

                if (File.Exists(path))
                {
                    if (config.overwrite)
                    {
                        Console.Write($"OVERWRITING! ");
                    } else
                    {
                        Console.WriteLine("Exists, skipping...");
                        continue;
                    }
                }

                ExportTexture(new UmaAssetBundleStream(abPath, bg.Value.FKey), exportType, path, config.quality);

                Console.WriteLine($"Written to {path}");
                successCount++;
            }

            catch (System.IO.DirectoryNotFoundException)
            {
                Console.WriteLine($"\nINFO: Directory \"{config.exportPath}\" not found. Creating one.");
                Directory.CreateDirectory(config.exportPath);
                Console.Write($"Retrying {bg.Key}...");

                ExportTexture(new UmaAssetBundleStream(abPath, bg.Value.FKey), exportType, path, config.quality);

                Console.WriteLine($"Written to {path}");
                successCount++;
            }

            catch (Exception e)
            {
                Console.WriteLine($"ERROR Loading {bg.Key}: {e}");
            }

            finally
            {
                iterations++;
            }
        }

        stopwatch.Stop();
        Console.WriteLine($"\nExported {successCount} out of {bgList.Count()}\nExecution time: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedMilliseconds/1000} s)");
    }

}