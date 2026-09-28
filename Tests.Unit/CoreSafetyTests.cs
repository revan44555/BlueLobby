using System;
using System.IO;
using BlueLobby.Core;
using Xunit;

namespace BlueLobby.UnitTests;

public sealed class CoreSafetyTests
{
    [Fact] public void PathSafety_RejectsOutsidePath()
    {
        string root=Path.Combine(Path.GetTempPath(),"BlueLobbyUnit-"+Guid.NewGuid().ToString("N"));
        Assert.True(PathSafety.IsInside(root,Path.Combine(root,"game.exe")));
        Assert.False(PathSafety.IsInside(root,root+"-outside/game.dll"));
    }

    [Fact] public void BinaryArchitecture_DetectsPeAndElf()
    {
        Assert.Equal(BinaryArchitecture.X64,BinaryArchitectureDetector.Detect(BuildPe(0x8664)));
        Assert.Equal(BinaryArchitecture.X86,BinaryArchitectureDetector.Detect(BuildPe(0x014c)));
        Assert.Equal(BinaryArchitecture.X64,BinaryArchitectureDetector.Detect(BuildElf(0x3E)));
    }

    [Fact] public void CompatDatabase_KeepsSeedDataOnInvalidJson()
    {
        var db=new CompatDatabase(); int count=db.All().Count;
        db.LoadFromJson("not-json");
        Assert.Equal(count,db.All().Count);
    }

    private static byte[] BuildPe(ushort machine)
    {
        byte[] d=new byte[512]; d[0]=(byte)'M'; d[1]=(byte)'Z';
        BitConverter.GetBytes(0x80).CopyTo(d,0x3C); d[0x80]=(byte)'P'; d[0x81]=(byte)'E';
        BitConverter.GetBytes(machine).CopyTo(d,0x84); return d;
    }

    private static byte[] BuildElf(ushort machine)
    {
        byte[] d=new byte[128]; d[0]=0x7F; d[1]=(byte)'E'; d[2]=(byte)'L'; d[3]=(byte)'F'; d[4]=2; d[5]=1;
        BitConverter.GetBytes(machine).CopyTo(d,18); return d;
    }
}
