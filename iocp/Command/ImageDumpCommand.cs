using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeruTeruServer.Common.Protocol;
using TeruTeruServer.Common.Enums;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.Command
{
    /// <summary>
    /// 수신 대기 중인 이미지 데이터를 파일로 덤프하는 명령어 클래스입니다.
    /// </summary>
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

                if (sendImageData.ImgSize < sendImageData.Data.Length)
                {
                    byte[] imgByte = new byte[sendImageData.ImgSize];
                    Array.Copy(sendImageData.Data, imgByte, sendImageData.ImgSize);
                    File.WriteAllBytes(filePath, imgByte);
                }
                else
                {
                    File.WriteAllBytes(filePath, sendImageData.Data);
                }

                Count++;
            }
            return true; // 프로그램 계속
        }
    }
}
