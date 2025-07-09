using Godot;
using Godot.Collections;
using System;

public static class ResourceLoading
{
    public static Array<String> GetAllFilesOfExt(string startPath, string ext)
    {
        Array<String> paths = new Array<String>();
        DirAccess dir = DirAccess.Open(startPath);
        dir.ListDirBegin();
        var fileName = dir.GetNext();
        while (fileName != "")
        {
            string filePath = startPath + "/" + fileName;
            if (dir.CurrentIsDir())
            {
                Array<String> app = GetAllFilesOfExt(filePath,ext);
                foreach (String p in app)
                {
                    paths.Add(p);
                }
            }
            else
            {
                string extension = filePath.GetExtension();
                if (extension == ext) 
                {
                    paths.Add(filePath); 
                }
            }
            fileName = dir.GetNext();
        }
        return paths;
    }
	public static Array<Resource> GetAllResourceOfType<T>(string startPath)
	{
        Array<Resource> filteredResources = new Array<Resource>();
    	Array<String> Tresources = GetAllFilesOfExt(startPath, "tres");
        Array<String> resF = GetAllFilesOfExt(startPath, "res");
        Tresources.AddRange(resF);
        foreach (string r in Tresources)
        {
            Resource res = GD.Load(r);
            if (res is T)
            {
                filteredResources.Add(res);
            }   
        }
        return filteredResources;
    }
}
