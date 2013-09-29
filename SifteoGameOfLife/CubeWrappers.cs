using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sifteo;

namespace SifteoCubeOfLife {

    class CubeWrapper {

        private const int GLIDER_SIZE = 5;
        private static int[,] GLIDER = new int[GLIDER_SIZE, 2] { { 0, 2 }, { 1, 2 }, { 2, 2 }, { 2, 1 }, { 1, 0 } };

        private const int DEAD_CELL = 255;
        private const int PIXEL_WIDTH = 8;
        private const int PIXEL_COUNT = (Cube.SCREEN_WIDTH / PIXEL_WIDTH) + 2; // + 2 for wrap-around

        private static Random mRandom = new Random();

        private Cube mCube;
        private int mTiltX = 0;
        private int mTiltY = 0;
        private bool mShaking = false;
        private int[,] mCurrentState = new int[PIXEL_COUNT, PIXEL_COUNT];
        private int[,] mPreviousState = new int[PIXEL_COUNT, PIXEL_COUNT];


        public CubeWrapper(Cube cube) {
            mCube = cube;
            mCube.userData = this;

            // Events
            mCube.ButtonEvent += OnButton;
            mCube.FlipEvent += OnFlip;
            mCube.TiltEvent += OnTilt;
            mCube.ShakeStartedEvent += OnShakeStarted;
            mCube.ShakeStoppedEvent += OnShakeStopped;

            Reset();
        }

        private void OnButton(Cube cube, bool pressed) {
            if (pressed) {
                // Add random glider
                Glider(); 
            }
        }

        private void OnFlip(Cube cube, bool isUp) {
            if (!isUp) {
                // Kill all cells
                Reset(); 
            }
        }

        private void OnTilt(Cube c, int x, int y, int z) {
            // Shift cells in direction of tilt
            mTiltX = y-1;
            mTiltY = x-1;
        }

        private void OnShakeStarted(Cube c) {
            // Start adding random cells
            mShaking = true;
        }

        private void OnShakeStopped(Cube c, int duration) {
            // Stop adding random cells
            mShaking = false;
        }

        public void Glider() {

            int i = mRandom.Next(1,PIXEL_COUNT-5);
            int j = mRandom.Next(1,PIXEL_COUNT-5);

            for (int x = 0; x < GLIDER_SIZE; x++) {
                mPreviousState[i + GLIDER[x, 0], j + GLIDER[x, 1]] = 0;
                PaintCell(i + GLIDER[x, 0], j + GLIDER[x, 1], mPreviousState[i + GLIDER[x, 0], j + GLIDER[x, 1]]);
            }

        }

        public void Reset() {
            for (int x = 0; x < PIXEL_COUNT; x++) {
                for (int y = 0; y < PIXEL_COUNT; y++) {
                    mCurrentState[x, y] = DEAD_CELL;
                    mPreviousState[x, y] = DEAD_CELL;
                }
            }
            mCube.FillScreen(Color.White);
            Paint();
        }

        public void Paint() {
            mCube.Paint();
        }

        public void Seed() {
            int i = 0;
            while (i++ < (PIXEL_COUNT * PIXEL_COUNT) / 5) {
                int x = mRandom.Next(1,PIXEL_COUNT-1);
                int y = mRandom.Next(1,PIXEL_COUNT-1);
                mPreviousState[x, y] = mRandom.Next(255);
                PaintCell(x, y, mPreviousState[x, y]);
            }
        }

        public void Tick() {
            for (int x = 1; x < PIXEL_COUNT-1; x++) {
                for (int y = 1; y < PIXEL_COUNT-1; y++) {
                    mCurrentState[x, y] = NextState(x, y);
                    if (mCurrentState[x, y] != mPreviousState[x, y]) {
                        PaintCell(x, y, mCurrentState[x, y]);
                    }
                }
            }
        }

        public bool ShouldFlip(Cube.Side side1, Cube.Side side2) {
            return ((Math.Abs(side1 - side2) == 1) && (int)side1 + (int)side2 != 3) || (Math.Abs(side1 - side2) == 2);
        }

        public void Update() {
            // Update displayed cells
            for (int x = 1; x < PIXEL_COUNT-1; x++) {
                for (int y = 1; y < PIXEL_COUNT-1; y++) {
                    mPreviousState[x,y] = mCurrentState[x,y];
                }
            }

            Pair<CubeWrapper, Cube.Side> cubeTop = GetNeighbourCube(Cube.Side.TOP);
            Pair<CubeWrapper, Cube.Side> cubeBottom = GetNeighbourCube(Cube.Side.BOTTOM);

            // Wrap top and bottom rows
            for (int x=1; x<PIXEL_COUNT-1; x++) {
                // TODO: transform mCurrentState based on orientation
                //bool shouldFlip = ((Math.Abs(Cube.Side.TOP - cubeTop.Second) == 1) && (Math.Abs(Cube.Side.BOTTOM - cubeBottom.Second) == 1)) ||
                //                  ((Math.Abs(Cube.Side.TOP - cubeTop.Second) == 2) && (Math.Abs(Cube.Side.BOTTOM - cubeBottom.Second) == 2));
                bool shouldFlipTop = (Math.Abs(Cube.Side.TOP - cubeTop.Second) == 1) || (Math.Abs(Cube.Side.TOP - cubeTop.Second) == 2);
                bool shouldFlipBottom = (Math.Abs(Cube.Side.BOTTOM - cubeBottom.Second) == 1) || (Math.Abs(Cube.Side.BOTTOM - cubeBottom.Second) == 2);
                mPreviousState[x, 0] = GetCellFromRow(cubeTop.First, cubeTop.Second, x, ShouldFlip(Cube.Side.TOP, cubeTop.Second));
                mPreviousState[x, PIXEL_COUNT - 1] = GetCellFromRow(cubeBottom.First, cubeBottom.Second, x, ShouldFlip(Cube.Side.BOTTOM, cubeBottom.Second));

                //mPreviousState[x, 0] = cubeTop.First.mCurrentState[x, PIXEL_COUNT-2];
                //mPreviousState[x, PIXEL_COUNT-1] = cubeBottom.First.mCurrentState[x, 1];
            }

            Pair<CubeWrapper, Cube.Side> cubeLeft = GetNeighbourCube(Cube.Side.LEFT);
            Pair<CubeWrapper, Cube.Side> cubeRight = GetNeighbourCube(Cube.Side.RIGHT);

            // Wrap left and right columns
            for (int y = 1; y < PIXEL_COUNT - 1; y++) {
                // TODO: transform mCurrentState based on orientation
                //bool shouldFlip = ((Math.Abs(Cube.Side.LEFT - cubeLeft.Second) == 1) && (Math.Abs(Cube.Side.RIGHT - cubeRight.Second) == 1)) ||
                //                  ((Math.Abs(Cube.Side.LEFT - cubeLeft.Second) == 2) && (Math.Abs(Cube.Side.RIGHT - cubeRight.Second) == 2));
                bool shouldFlipLeft = (Math.Abs(Cube.Side.LEFT - cubeLeft.Second) == 1) || (Math.Abs(Cube.Side.LEFT - cubeLeft.Second) == 2);
                bool shouldFlipRight = (Math.Abs(Cube.Side.RIGHT - cubeRight.Second) == 1) || (Math.Abs(Cube.Side.RIGHT - cubeRight.Second) == 2);
                mPreviousState[0, y] = GetCellFromRow(cubeLeft.First, cubeLeft.Second, y, ShouldFlip(Cube.Side.LEFT, cubeLeft.Second));
                mPreviousState[PIXEL_COUNT - 1, y] = GetCellFromRow(cubeRight.First, cubeRight.Second, y, ShouldFlip(Cube.Side.RIGHT, cubeRight.Second));
            
                //mPreviousState[0, y] = cubeLeft.First.mCurrentState[PIXEL_COUNT - 2, y];
                //mPreviousState[PIXEL_COUNT - 1, y] = cubeRight.First.mCurrentState[1, y];
            }

            // Wrap top left and bottom right
            //mPreviousState[0, 0] = mCurrentState[PIXEL_COUNT - 2, PIXEL_COUNT - 2];
            //mPreviousState[PIXEL_COUNT - 1, PIXEL_COUNT - 1] = mCurrentState[1, 1];

            // Wrap top right and bottom left
            //mPreviousState[PIXEL_COUNT - 1, 0] = mCurrentState[1, PIXEL_COUNT - 2];
            //mPreviousState[0, PIXEL_COUNT - 1] = mCurrentState[PIXEL_COUNT - 2, 1];
        }

        private int GetCellFromRow(CubeWrapper cube, Cube.Side side, int i, bool shouldFlip) {

            //if(cube.mCube != mCube)
            //Log.Debug("Side2 - Side1 " + side2 + " - " + side1 + " = " + (side2 - side1));

            i = !shouldFlip ? PIXEL_COUNT - i - 1: i;

            switch (side) {
                case Cube.Side.RIGHT:
                    return cube.mCurrentState[PIXEL_COUNT - 2, i];
                case Cube.Side.LEFT:
                    return cube.mCurrentState[1, i];
                case Cube.Side.BOTTOM:
                    return cube.mCurrentState[i, PIXEL_COUNT - 2];
                case Cube.Side.TOP:
                    return cube.mCurrentState[i, 1];
            }
            return 0;

        }


        // Get neighbour on provided side. If there is no immediate neighbour then  
        // wrap around and return the furthest away module on the opposite side
        private Pair<CubeWrapper,Cube.Side> GetNeighbourCube(Cube.Side side) {
            Cube cube = NeighbourOnSide(mCube, side);

            if (cube == null) {
                cube = mCube;
                side = Sifteo.Util.CubeHelper.InvertSide(side);
                Cube next = NeighbourOnSide(cube, side);
                while (next != null) {
                    side = Sifteo.Util.CubeHelper.RotateFromOriginToNeighbor(side, cube, next);
                    cube = next;
                    next = NeighbourOnSide(cube, side);
                }
            } else {
                side = Sifteo.Util.CubeHelper.InvertSide(
                    Sifteo.Util.CubeHelper.RotateFromOriginToNeighbor(side,mCube,cube));
            }
            return new Pair<CubeWrapper,Cube.Side>((CubeWrapper)cube.userData,side);
        }

        public int NextState(int x, int y) {
            List<int> neighbours = GetNeighbours(x, y);

            if (mPreviousState[x, y] < DEAD_CELL) {
                if(neighbours.Count < 2) {
                    return DEAD_CELL;
                } else if (neighbours.Count < 4) {
                    return mPreviousState[x, y];
                } else {
                    return DEAD_CELL;
                }
            } else {
                if (neighbours.Count == 3) {
                    return neighbours.Sum() / neighbours.Count;
                } else if(mShaking && mRandom.Next(10) == 0) {
                    return mRandom.Next(255);
                }
            }
            return mPreviousState[x, y];
        }

        private List<int> GetNeighbours(int x, int y) {
            List<int> neighbours = new List<int>();

            for (int i = x - 1; i <= x + 1; i++) {
                for (int j = y - 1; j <= y + 1; j++) {
                    if (!(x == i && y == j) && mPreviousState[i, j] < DEAD_CELL) {
                        neighbours.Add(mPreviousState[i, j]);
                    }
                }
            }
            return neighbours;
        }

        private void PaintCell(int x, int y, int live) {
            mCube.FillRect(new Color(live), (x-1) * PIXEL_WIDTH, (y-1) * PIXEL_WIDTH, PIXEL_WIDTH, PIXEL_WIDTH);
        }

        private static Cube NeighbourOnSide(Cube cube, Cube.Side side) {

            switch (side) {
                case Cube.Side.TOP:
                    return cube.Neighbors.Top;
                case Cube.Side.BOTTOM:
                    return cube.Neighbors.Bottom;
                case Cube.Side.LEFT:
                    return cube.Neighbors.Left;
                case Cube.Side.RIGHT:
                    return cube.Neighbors.Right;
                default:
                    return null;
            }
        }
    }

    public class Pair<T, U> {
        public Pair() {
        }

        public Pair(T first, U second) {
            this.First = first;
            this.Second = second;
        }

        public T First { get; set; }
        public U Second { get; set; }
    };
}
