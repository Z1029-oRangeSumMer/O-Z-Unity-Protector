﻿using O_Z_IL2CPP_Security;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using LitJson;
List<byte[]> StringLiteraBytes = new List<byte[]>();
List<byte[]> StringLiteraBytes_Crypted = new List<byte[]>();
string OpenFilePath;
byte[]? metadata_origin = null;

Console.WriteLine("O&Z_IL2CPP_Security");

if (args.Length == 0)
{
    Help();
    return;
}

if (!File.Exists("Config.json"))
{
    Console.WriteLine("Config.json not found!");
    Console.WriteLine("正在生成默认配置文件...");
    JsonIndex index = new JsonIndex()
    {
        key = 114514,
        Version = "24.4"
        
    };
    File.WriteAllText("Config.json", JsonMapper.ToJson(index));
    if (File.Exists("Config.json")) Console.WriteLine("已重新生成默认配置文件,..Done!");
}
JsonManager jsonManager = new JsonManager("Config.json");

if (args[0] == "Generate")
{
    _Generate();
    Console.WriteLine("Done!");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    return;
}

OpenFilePath = args[0];

Console.WriteLine("Loading Meatadata:" + OpenFilePath);

switch(args[1])
{
    case "Crypt":_Crypt();break;
    case "Decrypt":return;
    case "Read":_Read();break;
    case "Test":_Test();break;
    case "CheckVersion": CheckVersion(); break;
    default:_default();break;
}
return;
void _Crypt()
{
    Console.WriteLine("正在执行加密...");
    IL2CPP_Version ver;
    if (!File.Exists(OpenFilePath))
    {
        Console.WriteLine("File is not EXISTS!");
        return;
    }
    metadata_origin = File.ReadAllBytes(OpenFilePath);
    if (!CheckMetadataFile())
    {
        Console.WriteLine("这不是一个Metadata文件!");
        return; 
    }
    jsonManager = new JsonManager("Config.json");
    
    switch(jsonManager.index.Version)
    {
        case "24.4":
            { 
                ver = IL2CPP_Version.V24_4;
                Console.WriteLine("Metadata版本:24.4");
            }
            break;
        case "28":
            { 
                ver = IL2CPP_Version.V28;
                Console.WriteLine("Metadata版本:28");
            }
            break;
        default: Console.WriteLine("版本错误!请确保你配置了正确且受支持的Metadata版本!(24.4 / 28)"); return;
    }
    object Loader;
    if (ver == IL2CPP_Version.V24_4)
        Loader = new LoadMetadata_v24_5(new MemoryStream(metadata_origin));
    else if (ver == IL2CPP_Version.V28)
        Loader = new LoadMetadata_v28(new MemoryStream(metadata_origin));
    else
    {
        Console.WriteLine("Input version Error!");
        return;
    }
    Console.WriteLine("正在创建O&Z Metadata...");
    Metadata metadata = new Metadata(Loader.GetType().GetField("metadatastream").GetValue(Loader) as Stream, Loader.GetType().GetField("Header").GetValue(Loader).GetType(), Loader.GetType().GetField("Header").GetValue(Loader), ver);
    Console.WriteLine("正在加密StringLiteral...");
    StringLiteraBytes = metadata.GetBytesFromStringLiteral(metadata.stringLiterals);
    Console.WriteLine("正在加密String...");
    StringLiteraBytes_Crypted = Crypt.Cryptstring(StringLiteraBytes,jsonManager.index.key);
    byte[] allstring = metadata.GetAllStringFromMeta();
    Console.WriteLine("正在构建新的Metadata文件...");
    Stream stream = metadata.SetCryptedStreamToMetadata(StringLiteraBytes_Crypted, Crypt.CryptWithSkipNULL(allstring,(byte)Tools.CheckNull(jsonManager.index.key), jsonManager.index.key),ver);
    byte[] tmp = Tools.StreamToBytes(stream);
    Console.WriteLine("正在写入文件...");
    File.WriteAllBytes(args[2], tmp);
    Console.WriteLine("Done!");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
void _default()
{
    Console.WriteLine("parameter ERROR!");
    Help();
    return;
}
void _Read()
{
    return;
}
void _Generate()
{
    jsonManager = new JsonManager("Config.json");
    string src;
    CPP cpp;
    Console.WriteLine("正在创建密钥组件...");
    Console.WriteLine("您的Metadata版本为:" + jsonManager.index.Version);
    Console.WriteLine("您的密钥为:" + jsonManager.index.key);
    if (!Directory.Exists("Generation/" + jsonManager.index.Version + "/libil2cpp/vm/"))
        Directory.CreateDirectory("Generation/" + jsonManager.index.Version + "/libil2cpp/vm/");
    if (jsonManager.index.Version == "24.4")
    {
        src = File.ReadAllText("src-res/" + jsonManager.index.Version + "/MetadataCache.cpp");
        cpp = new CPP(src, IL2CPP_Version.V24_4, jsonManager.index.key, (byte)Tools.CheckNull(jsonManager.index.key));
        File.WriteAllText("Generation/" + jsonManager.index.Version + "/libil2cpp/vm/MetadataCache.cpp", cpp.retsrc);
        File.WriteAllLines("Generation/" + jsonManager.index.Version + "/libil2cpp/il2cpp-metadata.h", File.ReadAllLines("src-res/" + jsonManager.index.Version + "/il2cpp-metadata.h"));
    }
    else if (jsonManager.index.Version == "28")
    {
        src = File.ReadAllText("src-res/" + jsonManager.index.Version + "/GlobalMetadata.cpp");
        cpp = new CPP(src, IL2CPP_Version.V24_4, jsonManager.index.key, (byte)Tools.CheckNull(jsonManager.index.key));
        File.WriteAllText("Generation/" + jsonManager.index.Version + "/libil2cpp/vm/GlobalMetadata.cpp", cpp.retsrc);
        File.WriteAllLines("Generation/" + jsonManager.index.Version + "/libil2cpp/vm/GlobalMetadataFileInternals.h", File.ReadAllLines("src-res/" + jsonManager.index.Version + "/GlobalMetadataFileInternals.h"));
    }
    else
    {
        Console.WriteLine("版本错误!请确保你配置了正确且受支持的Metadata版本!(24.4 / 28)");
        return;
    }
    File.WriteAllLines("Generation/" + jsonManager.index.Version + "/libil2cpp/vm/xxtea.cpp", File.ReadAllLines("src-res/xxtea.cpp"));
    File.WriteAllLines("Generation/" + jsonManager.index.Version + "/libil2cpp/vm/xxtea.h", File.ReadAllLines("src-res/xxtea.h"));
    return;
}
bool CheckMetadataFile()
{
    if (BitConverter.ToUInt32(metadata_origin, 0) != 4205910959)
    {
        return false;
    }
    else
        return true;
}
void _Test()
{
    jsonManager = new JsonManager("Config.json");
    Console.WriteLine("Your Password is: " + jsonManager.index.key);
    Console.WriteLine("Your MetadataVersion is: " + jsonManager.index.Version);
    string tmp = File.ReadAllText("src-res/24.4/MetadataCache.cpp");
    CPP cpp = new CPP(tmp,IL2CPP_Version.V24_4,jsonManager.index.key, (byte)Tools.CheckNull(jsonManager.index.key));
    
    Console.WriteLine(cpp.retsrc);
}
void CheckVersion()
{
    metadata_origin = File.ReadAllBytes(OpenFilePath);
    if (!CheckMetadataFile()) return;
    MetadataCheck metadataCheck = new MetadataCheck(new MemoryStream(metadata_origin));
    Console.WriteLine("Your Metadata Version:" + metadataCheck.Version);
}
void Help()
{
    Console.WriteLine("使用方法:"+"\n");
    Console.WriteLine("加密:\n    O&Z_IL2CPP_Security.exe [文件路径] Crypt [输出路径]\n");
    Console.WriteLine("查看Metadata版本:\n    O&Z_IL2CPP_Security.exe [文件路径] CheckVersion\n");
    Console.WriteLine("生成密钥组件:\n    O&Z_IL2CPP_Security.exe Generate\n");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
