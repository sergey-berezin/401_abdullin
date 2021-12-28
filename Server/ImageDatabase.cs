using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace ImageDatabase
{
public class ProcessedImage
{
    public int Id { get; set; } // primary key
    public int Hash { get; set; }
    public byte[] Bitmap { get; set; }
    virtual public ICollection<RecognizedObject> RecognizedObjects { get; set; }

    public ProcessedImage(string file)
    {
        ImageConverter converter = new ImageConverter();
        Bitmap = (byte[]) converter.ConvertTo(Image.FromFile(file), typeof(byte[]));
        ComputeHash();
        RecognizedObjects = new List<RecognizedObject>();
    }

    public ProcessedImage()
    {}

    private void ComputeHash()
    {
        // Modified FNV-1 hash algorithm
        byte hash = 0xe4;
        foreach (byte data in Bitmap)
        {
            hash *= 0xb3;
            hash ^= data;
        }
        Hash = (int) hash;
    }

    public static bool operator==(ProcessedImage a, ProcessedImage b)
    {
        if (a.Hash != b.Hash || a.Bitmap.Length != b.Bitmap.Length)
            return false;
        for (int i = 0; i < a.Bitmap.Length; ++i)
        {
            if (a.Bitmap[i] != b.Bitmap[i])
                return false;
        }
        return true;
    }

    public static bool operator!=(ProcessedImage a, ProcessedImage b)
    {
        if (a.Hash != b.Hash && a.Bitmap.Length != b.Bitmap.Length)
            return true;
        for (int i = 0; i < a.Bitmap.Length; ++i)
        {
            if (a.Bitmap[i] != b.Bitmap[i])
                return true;
        }
        return false;
    }

    public override bool Equals(Object obj)
    {
        if (obj == null || GetType() != obj.GetType()) return false;
        ProcessedImage b = (ProcessedImage) obj;
        if (Hash != b.Hash || this.Bitmap.Length != b.Bitmap.Length)
            return false;
        for (int i = 0; i < this.Bitmap.Length; ++i)
        {
            if (this.Bitmap[i] != b.Bitmap[i])
                return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        return Hash;
    }
}


public class RecognizedObject
{
    public int Id { get; set; } // primary key
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
    public string Label { get; set; }

    public int ProcessedImageId { get; set; } // foreign key

    public RecognizedObject(float x1, float y1, float x2, float y2, string label)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        Label = label;
    }
}


public class DatabaseStoreContext : DbContext
{
    public DbSet<ProcessedImage> ProcessedImages { get; set; }
    public DbSet<RecognizedObject> RecognizedObjects { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseLazyLoadingProxies().UseSqlite("Data Source=image_objects.db");

    public void AddProcessedImage(ProcessedImage img_to_add)
    {
        foreach (var img in ProcessedImages)
        {
            if (img_to_add == img)
            {
                return;
            }
        }
        Add(img_to_add);
        SaveChanges();
    }
}
}
