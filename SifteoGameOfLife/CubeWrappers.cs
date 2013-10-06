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
                mPreviousState[x, 0] = GetCellFromRow(cubeTop.First, cubeTop.Second, x, 
                    ShouldFlip(Cube.Side.TOP, cubeTop.Second));
                mPreviousState[x, PIXEL_COUNT - 1] = GetCellFromRow(cubeBottom.First, cubeBottom.Second, x, 
                    ShouldFlip(Cube.Side.BOTTOM, cubeBottom.Second));
            }

            Pair<CubeWrapper, Cube.Side> cubeLeft = GetNeighbourCube(Cube.Side.LEFT);
            Pair<CubeWrapper, Cube.Side> cubeRight = GetNeighbourCube(Cube.Side.RIGHT);

            // Wrap left and right columns
            for (int y = 1; y < PIXEL_COUNT - 1; y++) {
                mPreviousState[0, y] = GetCellFromRow(cubeLeft.First, cubeLeft.Second, y, 
                    ShouldFlip(Cube.Side.LEFT, cubeLeft.Second));
                mPreviousState[PIXEL_COUNT - 1, y] = GetCellFromRow(cubeRight.First, cubeRight.Second, y, 
                    ShouldFlip(Cube.Side.RIGHT, cubeRight.Second));
            }

			Triple<CubeWrapper, Cube.Side, Cube.Side> cubeTopLeft = GetDiagonalNeighbourCube (Cube.Side.LEFT, Cube.Side.TOP);
			Triple<CubeWrapper, Cube.Side, Cube.Side> cubebottomRight = GetDiagonalNeighbourCube (Cube.Side.RIGHT, Cube.Side.BOTTOM);

			if (cubeTopLeft.First != null) {
				if (cubeTopLeft.First.mCube != mCube)
					Log.Debug ("TopLeft:" + mCube.UniqueId + " " + cubeTopLeft.First.mCube.UniqueId + " " + cubeTopLeft.Second + " " + cubeTopLeft.Third);
				mPreviousState [0, 0] = GetCellFromDiagonal (cubeTopLeft.First, cubeTopLeft.Second, cubeTopLeft.Third);
			}

			if (cubebottomRight.First != null) {
				if (cubebottomRight.First.mCube != mCube)
					Log.Debug ("bottomRight:" + mCube.UniqueId + " " + cubebottomRight.First.mCube.UniqueId + " " + cubebottomRight.Second + " " + cubebottomRight.Third);
				mPreviousState[PIXEL_COUNT - 1, PIXEL_COUNT - 1] =  GetCellFromDiagonal (cubebottomRight.First, cubebottomRight.Second, cubebottomRight.Third);
			}


			Triple<CubeWrapper, Cube.Side, Cube.Side> cubeTopRight = GetDiagonalNeighbourCube (Cube.Side.RIGHT, Cube.Side.TOP);
			Triple<CubeWrapper, Cube.Side, Cube.Side> cubeBottomLeft = GetDiagonalNeighbourCube (Cube.Side.LEFT, Cube.Side.BOTTOM);


			if (cubeTopRight.First != null) {
				if (cubeTopRight.First.mCube != mCube)
					Log.Debug ("TopRight:" + mCube.UniqueId + " " + cubeTopRight.First.mCube.UniqueId + " " + cubeTopRight.Second + " " + cubeTopRight.Third);
				mPreviousState [PIXEL_COUNT - 1, 0] = GetCellFromDiagonal (cubeTopRight.First, cubeTopRight.Second, cubeTopRight.Third);
			}

			if (cubeBottomLeft.First != null) {
				if (cubeBottomLeft.First.mCube != mCube)
					Log.Debug ("bottomLeft:" + mCube.UniqueId + " " + cubeBottomLeft.First.mCube.UniqueId + " " + cubeBottomLeft.Second + " " + cubeBottomLeft.Third);
				mPreviousState[0, PIXEL_COUNT - 1] =  GetCellFromDiagonal (cubeBottomLeft.First, cubeBottomLeft.Second, cubeBottomLeft.Third);
			}
        }

		private int GetCellFromDiagonal(CubeWrapper cube, Cube.Side side1, Cube.Side side2) {

			if (((side1 == Cube.Side.TOP) && (side2 == Cube.Side.LEFT)) || 
				((side1 == Cube.Side.LEFT) && (side2 == Cube.Side.TOP))) {
				return cube.mCurrentState [1, 1];
			} else if ((side1 == Cube.Side.BOTTOM && side2 == Cube.Side.RIGHT) || 
					   (side1 == Cube.Side.RIGHT && side2 == Cube.Side.BOTTOM)) {
				return cube.mCurrentState [PIXEL_COUNT - 2, PIXEL_COUNT - 2];
			} else if (((side1 == Cube.Side.TOP) && (side2 == Cube.Side.RIGHT)) || 
					   ((side1 == Cube.Side.RIGHT) && (side2 == Cube.Side.TOP))) {
				return cube.mCurrentState [PIXEL_COUNT - 2, 1];
			} else if ((side1 == Cube.Side.BOTTOM && side2 == Cube.Side.LEFT) || 
					   (side1 == Cube.Side.LEFT && side2 == Cube.Side.BOTTOM)) {
				return cube.mCurrentState [1, PIXEL_COUNT - 2];
			}
			return 255;
		}

        private int GetCellFromRow(CubeWrapper cube, Cube.Side side, int i, bool shouldFlip) {

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

		private Triple<CubeWrapper,Cube.Side,Cube.Side> GetDiagonalNeighbourCube(Cube.Side side1, Cube.Side side2) {

			Cube cube = NeighbourOnDiagonal(mCube, side1, side2);

			if (cube == null) {
				cube = mCube;
				side1 = Sifteo.Util.CubeHelper.InvertSide (side1);
				side2 = Sifteo.Util.CubeHelper.InvertSide (side2);
				Cube next = NeighbourOnDiagonal (cube, side1, side2);
				while (next != null) {
					side1 = Sifteo.Util.CubeHelper.RotateFromOriginToNeighbor (side1, cube, next);
					side2 = Sifteo.Util.CubeHelper.RotateFromOriginToNeighbor (side2, cube, next);
					cube = next;
					next = NeighbourOnDiagonal (cube, side1, side2);
				}
			} else {
			
				side1 = Sifteo.Util.CubeHelper.InvertSide (
					Sifteo.Util.CubeHelper.RotateFromOriginToNeighbor (side1, mCube, cube));
				side2 = Sifteo.Util.CubeHelper.InvertSide (
					Sifteo.Util.CubeHelper.RotateFromOriginToNeighbor (side2, mCube, cube));
			}

			return new Triple<CubeWrapper,Cube.Side,Cube.Side> ((CubeWrapper)cube.userData, side1, side2);
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

		private static Cube NeighbourOnDiagonal(Cube cube, Cube.Side side1, Cube.Side side2) {
			Cube neighbour = NeighbourOnSide(cube, side1);

			if (neighbour != null && NeighbourOnSide (neighbour, side2) != null) {
				return NeighbourOnSide (neighbour, side2);
			}

			neighbour = NeighbourOnSide(cube, side2);
			if (neighbour != null && NeighbourOnSide (neighbour, side1) != null) {
				return NeighbourOnSide (neighbour, side1);
			}

			return null;

			//if( (NeighbourOnSide(cube, side1) != null) && 
		//		((neighbour = NeighbourOnSide(NeighbourOnSide(cube, side1), side2)) != null)) {
	//			return neighbour;
	//		} else if( (NeighbourOnSide(cube, side2) != null) && 
	//			((neighbour = NeighbourOnSide(NeighbourOnSide(cube, side2), side1)) != null)) {
	//			return neighbour;
	//		} else {
	//			return null;
	//		}
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

	public class Triple<T, U, V> {
		public Triple() {
		}

		public Triple(T first, U second, V third) {
			this.First = first;
			this.Second = second;
			this.Third = third;
		}

		public T First { get; set; }
		public U Second { get; set; }
		public V Third { get; set; }
	};
}
