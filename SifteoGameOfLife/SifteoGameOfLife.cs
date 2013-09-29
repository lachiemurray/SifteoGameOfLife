using Sifteo;
using System;
using System.Collections.Generic;

namespace SifteoCubeOfLife
{
    public class SifteoGameOfLife : BaseApp
    {
        List<CubeWrapper> mCubeWrappers = new List<CubeWrapper>();

        public override int FrameRate {
            get { return 10; }
        }

        override public void Setup()
        {
            Log.Debug("Setup()");
            foreach (Cube cube in this.CubeSet) {
                mCubeWrappers.Add(new CubeWrapper(cube));
            }
        }

        override public void Tick()
        {
            foreach (CubeWrapper cubeWrapper in mCubeWrappers) {
                cubeWrapper.Tick();
            }

            foreach (CubeWrapper cubeWrapper in mCubeWrappers) {
                cubeWrapper.Paint();
                cubeWrapper.Update();
            }
        }

        // development mode only
        static void Main(string[] args) { new SifteoGameOfLife().Run(); }
    }
}

