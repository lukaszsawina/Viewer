using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Events;

public class CameraSourceChangeEvent
{
    public VideoCapture VideoCapture { get; set; }
    public CameraSourceChangeEvent(VideoCapture videoCapture)
    {
        VideoCapture = videoCapture;
    }
}
