using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    public class ComponentEdgeDetection : GH_Component {
        public ComponentEdgeDetection() : base(
            "Edge detection", "Edge detection",
            "Detekce hran dle matice",
            "Detector", "Modify image") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek (může být barevný)\nBitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number", "Number", "Velikost matice (3 nebo 5)\nNumber of size edge detection matrix (3 or 5), default is 3", GH_ParamAccess.item, 3);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            Bitmap bitmap=null;
            double number=3;

            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }
            if (!DA.GetData(1, ref number)) { 
                if (number!=3 || number!=5) {
                    DA.AbortComponentSolution();
                    return;
                }
            }

            // Výroba matice
            float[,] matrix=null;
            int matrixSize=1;

            if (number==3) { 
                matrix = new float[,] {
                    { 1.0f, 2.0f, 1.0f},
                    { 2.0f, 0.0f, 2.0f},
                    { 1.0f, 2.0f, 1.0f}
                };
                matrixSize=1;
            } else {
                matrix = new float[,] {
                    { 0.0f, 8.0f, 4.0f, 8.0f, 0.0f},
                    { 8.0f, 2.0f, 1.0f, 2.0f, 8.0f},
                    { 4.0f, 1.0f, 0.0f, 1.0f, 4.0f},
                    { 8.0f, 2.0f, 1.0f, 2.0f, 8.0f},
                    { 0.0f, 8.0f, 4.0f, 8.0f, 0.0f}
                };
                matrixSize=2;
            }
            int matrixLenX=matrix.GetLength(0), matrixLenY=matrix.GetLength(1);
            int matrixStartX=-matrixLenX/2, matrixStartY=-matrixLenY/2;
            int matrixEndX=matrixLenX+matrixStartX, matrixEndY=matrixLenY+matrixStartY;

            Rectangle recSize=new Rectangle(0,0, bitmap.Width, bitmap.Height);
            Bitmap bitmapNew=new Bitmap(recSize.Width, recSize.Height);

            BitmapData dataOriginal=bitmap.LockBits(recSize, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            byte* fromW=(byte*)dataNew.Scan0, fromO=(byte*)dataOriginal.Scan0;
          
            Parallel.For(matrixSize, recSize.Height-matrixSize, (y, state) => {
                for (int x=matrixSize; x<recSize.Width-matrixSize; x++) {
                    // Získání barvy aktuálního pixelu
                    int n1=GetPixelOrig(x, y, 0);
                    int n2=GetPixelOrig(x, y, 1);
                    int n3=GetPixelOrig(x, y, 2);
                    float forceTotal=0;
                    float forceCount=0;

                    // Výpočet pixelu
                    for (int mx=0; mx<matrixLenX; mx++) { 
                        int moveX=matrixStartX+mx;
                        int xx=x+moveX;

                        for (int my=0; my<matrixLenY; my++) {
                            int moveY=matrixStartY+my;
                                                        
                            if (moveX!=0 && moveY!=0) { // Kromě prostředního... 
                                float val=matrix[mx, my];
                                if (val==0) continue; // Rohy u 5x5 matice ne
                                int yy=y+moveY;
                                int c1=GetPixelOrig(xx, yy, 0)-n1;
                                int c2=GetPixelOrig(xx, yy, 1)-n2;
                                int c3=GetPixelOrig(xx, yy, 2)-n3;
                                if (c1<0)c1=-c1;
                                if (c2<0)c2=-c2;
                                if (c3<0)c3=-c3;
                                forceTotal+=(c1+c2+c3)/3f*val;
                                forceCount+=val;
                            }
                        }
                    }

                    SetPixelWrapAllCh(x, y, (byte)(forceTotal/forceCount+0.5f));
                }
            });

            // Funkce k zápisu čísla
            void SetPixelWrapAllCh(int x, int y, byte b) {
                byte* head=fromW+(y*recSize.Width+x)*3;
                *head = b;
                *(head+1) = b;
                *(head+2) = b;
            }

            // Získání barvy, ch=channel
            byte GetPixelOrig(int x, int y, int ch) => *(fromO+(y*recSize.Width+x)*3+ch);

            bitmap.UnlockBits(dataOriginal);
            bitmapNew.UnlockBits(dataNew);

            DA.SetData(0, bitmapNew);
        }

        protected override Bitmap Icon => Properties.Resources.edges;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C5-4FCA-9839-799AA04BFD56");
    }
}