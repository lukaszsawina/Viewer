using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Events;

public class RecordingChangeEvent
{
    public string RecordingPath { get; set; }

    public RecordingChangeEvent(string recordingPath)
    {
        RecordingPath = recordingPath;
    }
}
