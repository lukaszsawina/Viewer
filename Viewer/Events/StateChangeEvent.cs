using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Model;

namespace Viewer.Events;

public class StateChangeEvent
{
    public StateModel State { get; set; }
    public StateChangeEvent(StateModel state)
    {
        State = state;
    }
}
