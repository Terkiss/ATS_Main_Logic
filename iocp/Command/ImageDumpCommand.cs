using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeruTeruServer.ManageLogic.Protocol;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.Command
{
    public class ImageDumpCommand : ICommand
    {
        public bool Execute(string[] args)
        {
            int Count = 0;
            string path = @"Receve";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            while (ServerMemory.GetImageWork_PreOrder_Queue(out SendImageData sendImageData))
            {
                string fileName = $"image_{Count}.jpg";
                string filePath = Path.Combine(path, fileName);

                if (sendImageData.imgSize < sendImageData.data.Length)
                {
                    byte[] imgByte = new byte[sendImageData.imgSize];
                    Array.Copy(sendImageData.data, imgByte, sendImageData.imgSize);
                    File.WriteAllBytes(filePath, imgByte);
                }
                else
                {
                    File.WriteAllBytes(filePath, sendImageData.data);
                }

                Count++;
            }
            return true; // 프로그램 계속
        }
    }
}
