using System.Collections.Generic;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    internal unsafe class Analyzator {
        public static int PictureWidth, PictureHeight;
        public static byte* Pointer;
        public static int Stride;
        public static int NextPoint=1;

        static volatile List<DInt> Points;
        
        public static List<DInt> Analyze(int ignore) {
            Points = new List<DInt> {
                Capacity = 5000
            };

            // Průchod horizontálně a vertikálně
            // Dolů
            Parallel.ForEach(SteppedIterator(PictureWidth), x=> {
                bool isSomething=false;
                int startSomething=-1;

                for (int y=0; y<PictureHeight; y++) { 
                    int value= *(Pointer + Stride*y+x*3);

                    // Pixel je prázný
                    if (value<ignore) { 

                        // Přechchozí pixel byl bílý
                        if (isSomething) { 
                            isSomething=false;
                            // Aritmetics center of ...0001111000...  start and end
                            //                    means    /\     mean
                            // List neuzamkne obsah... zvýšena kapacita
                            lock (Points) Points.Add(new DInt(x, (startSomething+(y-1))/2));
                        }
                       
                    } else { 
                        // Pixel není prázný
                        if (!isSomething) { 
                            isSomething=true;
                            startSomething=y;
                        }
                    }
                }
            });
            
            // Doprava
            Parallel.ForEach(SteppedIterator(PictureHeight), y => {  
                bool isSomething=false;
                int startSomething=-1;
                byte* row=Pointer + Stride*y;

                for (int x=0; x<PictureWidth; x++) { 
                    int value= *(row+x*3);

                    // Bílý pixel
                    if (value<ignore) {
                        // Přechchozí pixel byl bílý
                        if (isSomething) { 
                            isSomething=false;
                            // Aritmetics center of ...0001111000...  start and end
                            //                    means    /\     mean
                            lock (Points)  Points.Add(new DInt((startSomething+(x-1))/2, y));
                        }
                    } else { 
                        // Pixel není prázný
                        if (!isSomething) { 
                            isSomething=true;
                            startSomething=x;
                        }
                    }
                }
            });

            return Points;
        }

        static int GetValue(int x, int y) => *(Pointer + Stride*y+x*3);

        private static IEnumerable<int> SteppedIterator(int endIndex) {
            for (int i=0; i<endIndex; i+=NextPoint) yield return i;
        }
    }
}