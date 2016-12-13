﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;

using SlimDX.Direct3D9;
using SlimDX;
using Microsoft.Kinect;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;
using VVVV.Utils;
using VVVV.MSKinect.Lib;

namespace VVVV.MSKinect.Nodes
{
    [PluginInfo(Name = "World", 
	            Category = "Kinect", 
	            Version = "Microsoft DX9", 
	            Author = "vux", 
	            Tags = "EX9, texture",
	            Help = "Returns a A32B32G32R32F formatted texture with world-space coordinates encoded in each pixel")]
    public unsafe class KinectWorldTextureNode : IPluginEvaluate, IPluginConnections, IPluginDXTexture2
    {
        //memcopy method
        [DllImport("Kernel32.dll", EntryPoint="RtlMoveMemory", SetLastError=false)]
        static extern void CopyMemory(IntPtr dest, IntPtr src, int size);
        
        [Input("Kinect Runtime")]
        private Pin<KinectRuntime> FInRuntime;

        private IDXTextureOut FOutTexture;

        [Output("Frame Index", IsSingle = true, Order = 10)]
        private ISpread<int> FOutFrameIndex;

        private int frameindex = -1;

        private bool FInvalidateConnect = false;
        private bool FInvalidate = true;

        private KinectRuntime runtime;

        private DepthImagePixel[] depthpixels;
        private SkeletonPoint[] skelpoints;
        
        private object m_lock = new object();

        private Dictionary<Device, Texture> FDepthTex = new Dictionary<Device, Texture>();

        [ImportingConstructor()]
        public KinectWorldTextureNode(IPluginHost host)
        {
            skelpoints = new SkeletonPoint[640 * 480];
            depthpixels = new DepthImagePixel[640 * 480];
                    
            host.CreateTextureOutput("Texture Out", TSliceMode.Single, TPinVisibility.True, out this.FOutTexture);
        }

        public void Evaluate(int SpreadMax)
        {
            if (this.FInvalidateConnect)
            {
                if (runtime != null)
                {
                    this.runtime.DepthFrameReady -= DepthFrameReady;
                }

                if (this.FInRuntime.PluginIO.IsConnected)
                {
                    //Cache runtime node
                    this.runtime = this.FInRuntime[0];

                    if (runtime != null)
                    {
                        this.FInRuntime[0].DepthFrameReady += DepthFrameReady;
                    }
                    
                }

                this.FInvalidateConnect = false;
            }

            this.FOutFrameIndex[0] = this.frameindex;
        }

        public void ConnectPin(IPluginIO pin)
        {
            if (pin == this.FInRuntime.PluginIO)
            {
                this.FInvalidateConnect = true;
            }
        }

        public void DisconnectPin(IPluginIO pin)
        {
            if (pin == this.FInRuntime.PluginIO)
            {
                this.FInvalidateConnect = true;
            }
        }

        public Texture GetTexture(IDXTextureOut ForPin, Device OnDevice, int Slice)
        {
            if (this.FDepthTex.ContainsKey(OnDevice)) 
            { 
            	return this.FDepthTex[OnDevice];
            }
            else
            	return null;
        }

        public void UpdateResource(IPluginOut ForPin, Device OnDevice)
        {
            if (this.runtime != null)
            {

                if (!this.FDepthTex.ContainsKey(OnDevice))
                {
                    Texture t = null;
                    if (OnDevice is DeviceEx)
                    {
                        t = new Texture(OnDevice, 640, 480, 1, Usage.Dynamic, Format.A32B32G32R32F, Pool.Default);
                    }
                    else
                    {
                       t = new Texture(OnDevice, 640, 480, 1, Usage.None, Format.A32B32G32R32F, Pool.Managed);
                    }
                    this.FDepthTex.Add(OnDevice, t);
                }

                if (this.FInvalidate)
                {
                    Texture tx = this.FDepthTex[OnDevice];
  
                    //lock the vvvv texture
                    DataRectangle rect;
                    if (tx.Device is DeviceEx)
                        rect = tx.LockRectangle(0, LockFlags.None);
                    else
                        rect = tx.LockRectangle(0, LockFlags.Discard);
                    
                    try
                    {
                        lock (this.m_lock)
                        {
                            var row = 640 * 16;
                            
                            fixed (SkeletonPoint* p = &this.skelpoints[0])
                            {
                                IntPtr src = new IntPtr(p);
                                //copy one row a time
                                for (int i = 0; i < 480; i++)
                                { 
                                    CopyMemory(rect.Data.DataPointer.Move(rect.Pitch * i), src, row);
                                    src = src.Move(row);
                                }
                            }
                        }
                    }
                    finally
                    {
                        tx.UnlockRectangle(0);
                    }
    
                    this.FInvalidate = false;
                }
            }
        }

        public void DestroyResource(IPluginOut ForPin, Device OnDevice, bool OnlyUnManaged)
        {
            if (this.FDepthTex.ContainsKey(OnDevice))
            {
                this.FDepthTex[OnDevice].Dispose();
                this.FDepthTex.Remove(OnDevice);
            }
        }

        private void DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            DepthImageFrame frame = e.OpenDepthImageFrame();

            if (frame != null)
            {
                if (frame.FrameNumber != this.frameindex)
                {
                    this.FInvalidate = true;
//                    this.RebuildBuffer(frame.Format, false);

                    this.frameindex = frame.FrameNumber;
                    frame.CopyDepthImagePixelDataTo(this.depthpixels);

                    lock (m_lock)
                    {
                        this.runtime.Runtime.CoordinateMapper.MapDepthFrameToSkeletonFrame(frame.Format, this.depthpixels, this.skelpoints);
                    }
                 }
                frame.Dispose();
            }
        }
    }
}
