#region licence/info

//////project name
//vvvv plugin template

//////description
//basic vvvv node plugin template.
//Copy this an rename it, to write your own plugin node.

//////licence
//GNU Lesser General Public License (LGPL)
//english: http://www.gnu.org/licenses/lgpl.html
//german: http://www.gnu.de/lgpl-ger.html

//////language/ide
//C# sharpdevelop

//////dependencies
//VVVV.PluginInterfaces.V1;
//VVVV.Utils.VColor;
//VVVV.Utils.VMath;

//////initial author
//vvvv group

#endregion licence/info

//use what you need
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.Generic;
using System.Collections;

using VVVV.PluginInterfaces.V1;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using SlimDX.Direct3D9;

//the vvvv node namespace
namespace VVVV.Nodes
{
	//class definition
	public class PluginMeshTemplate: IPlugin, IDisposable, IPluginDXMesh
	{
		#region field declaration
		
		//the host (mandatory)
		private IPluginHost FHost;
		//Track whether Dispose has been called.
		private bool FDisposed = false;
		
		private IDXMeshIO FMyMeshOutput;
		
		private Dictionary<int, Mesh> FDeviceMeshes = new Dictionary<int, Mesh>();
		
		#endregion field declaration
		
		#region constructor/destructor
		
		public PluginMeshTemplate()
		{
			//the nodes constructor
			//nothing to declare for this node
		}
		
		// Implementing IDisposable's Dispose method.
		// Do not make this method virtual.
		// A derived class should not be able to override this method.
		public void Dispose()
		{
			Dispose(true);
			// Take yourself off the Finalization queue
			// to prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		
		// Dispose(bool disposing) executes in two distinct scenarios.
		// If disposing equals true, the method has been called directly
		// or indirectly by a user's code. Managed and unmanaged resources
		// can be disposed.
		// If disposing equals false, the method has been called by the
		// runtime from inside the finalizer and you should not reference
		// other objects. Only unmanaged resources can be disposed.
		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if(!FDisposed)
			{
				if(disposing)
				{
					// Dispose managed resources.
					
				}
				// Release unmanaged resources. If disposing is false,
				// only the following code is executed.

				FHost.Log(TLogType.Debug, "PluginMeshTemplate is being deleted");
				
				// Note that this is not thread safe.
				// Another thread could start disposing the object
				// after the managed resources are disposed,
				// but before the disposed flag is set to true.
				// If thread safety is necessary, it must be
				// implemented by the client.
			}
			FDisposed = true;
		}

		// Use C# destructor syntax for finalization code.
		// This destructor will run only if the Dispose method
		// does not get called.
		// It gives your base class the opportunity to finalize.
		// Do not provide destructors in types derived from this class.
		~PluginMeshTemplate()
		{
			// Do not re-create Dispose clean-up code here.
			// Calling Dispose(false) is optimal in terms of
			// readability and maintainability.
			Dispose(false);
		}
		#endregion constructor/destructor
		
		#region node name and infos
		
		//provide node infos
		private static IPluginInfo FPluginInfo;
		public static IPluginInfo PluginInfo
		{
			get
			{
				if (FPluginInfo == null)
				{
					//fill out nodes info
					//see: http://www.vvvv.org/tiki-index.php?page=Conventions.NodeAndPinNaming
					FPluginInfo = new PluginInfo();
					
					//the nodes main name: use CamelCaps and no spaces
					FPluginInfo.Name = "Template";
					//the nodes category: try to use an existing one
					FPluginInfo.Category = "Template";
					//the nodes version: optional. leave blank if not
					//needed to distinguish two nodes of the same name and category
					FPluginInfo.Version = "Mesh";
					
					//the nodes author: your sign
					FPluginInfo.Author = "vvvv group";
					//describe the nodes function
					FPluginInfo.Help = "Offers a basic code layout to start from when writing a vvvv plugin";
					//specify a comma separated list of tags that describe the node
					FPluginInfo.Tags = "";
					
					//give credits to thirdparty code used
					FPluginInfo.Credits = "";
					//any known problems?
					FPluginInfo.Bugs = "";
					//any known usage of the node that may cause troubles?
					FPluginInfo.Warnings = "";
					
					//leave below as is
					System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
					System.Diagnostics.StackFrame sf = st.GetFrame(0);
					System.Reflection.MethodBase method = sf.GetMethod();
					FPluginInfo.Namespace = method.DeclaringType.Namespace;
					FPluginInfo.Class = method.DeclaringType.Name;
					//leave above as is
				}
				return FPluginInfo;
			}
		}

		public bool AutoEvaluate
		{
			//return true if this node needs to calculate every frame even if nobody asks for its output
			get {return false;}
		}
		
		#endregion node name and infos
		
		#region pin creation
		
		//this method is called by vvvv when the node is created
		public void SetPluginHost(IPluginHost Host)
		{
			//assign host
			FHost = Host;
			
			//create outputs
			FHost.CreateMeshOutput("Mesh", TSliceMode.Dynamic, TPinVisibility.True, out FMyMeshOutput);
		}

		#endregion pin creation
		
		#region mainloop
		
		public void Configurate(IPluginConfig Input)
		{
			//nothing to configure in this plugin
			//only used in conjunction with inputs of type cmpdConfigurate
		}
		
		//here we go, thats the method called by vvvv each frame
		//all data handling should be in here
		public void Evaluate(int SpreadMax)
		{
			FMyMeshOutput.SliceCount = SpreadMax;
		}
		
		#endregion mainloop
		
		#region DXMesh
		public void UpdateResource(IPluginOut ForPin, int OnDevice)
		{
			//this is called every frame and is a bit like Evaluate,
			//but it is called for every device. so here the plugin should only do things
			//that are device-specific and do preparing calculations in Evaluate still
			
			try
			{
				Mesh m = FDeviceMeshes[OnDevice];
			}
			catch
			{
				//if resource is not yet created on given Device, create it now
				FHost.Log(TLogType.Debug, "Creating Resource...");
				Device dev = Device.FromPointer(new IntPtr(OnDevice));
				FDeviceMeshes.Add(OnDevice, Mesh.CreateTeapot(dev));

				//dispose device
				dev.Dispose();
				//null device
				dev = null;
			}
		}
		
		public void DestroyResource(IPluginOut ForPin, int OnDevice, bool OnlyUnManaged)
		{
			//called when a resource needs to be disposed on a given device
			//this is also called when the plugin is destroyed,
			//so don't dispose dxresources in the plugins destructor/Dispose()
			try
			{
				Mesh m = FDeviceMeshes[OnDevice];
				FHost.Log(TLogType.Debug, "Destroying Resource...");
				FDeviceMeshes.Remove(OnDevice);
				
				//dispose meshs device
				m.Device.Dispose();
				//dispose mesh
				m.Dispose();
				//null mesh
				m = null;
			}
			catch
			{
				//nothing to do 
			}
		}
		
		public void GetMesh(IDXMeshIO ForPin, int OnDevice, out int MeshPointer)
		{
			//this is called from the plugin host from within directX beginscene/endscene
			//therefore the plugin shouldn't be doing much here other than handing back the right mesh
			
			MeshPointer = 0;
			//in case the plugin has several mesh outputpins a test for the pin can be made here to get the right mesh.
			if (ForPin == FMyMeshOutput)
			{
				Mesh m = FDeviceMeshes[OnDevice];
				if (m != null)
					MeshPointer = m.ComPointer.ToInt32();
			}
		}
		#endregion
	}
}
