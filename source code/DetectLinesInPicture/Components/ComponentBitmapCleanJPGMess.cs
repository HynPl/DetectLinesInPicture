using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    public class ComponentCleanJPGMess : GH_Component {
        public ComponentCleanJPGMess()
          : base("Clean JPG Noise", "Clean JPG Noise",
            "Smazat vyčnívající pixely, trochu pomáhá redukovat JPG šum\nReduce JPG noise",
            "Detector", "Modify image") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Černobílý obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Černobílý obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {

            Bitmap bitmap=null;
            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }

            int PictureWidth=bitmap.Width;
            int PictureHeight=bitmap.Height;
            Rectangle recSize=new Rectangle(0,0, PictureWidth, PictureHeight);

            Bitmap bitmapNew=bitmap.Clone(recSize,PixelFormat.Format24bppRgb);

            BitmapData dataOriginal=bitmap.LockBits(recSize, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            byte* fromW=(byte*)dataNew.Scan0, fromR=(byte*)dataOriginal.Scan0;

            // Okraje neřeš
            int matrixSize=1;
            int WidthMMatrixSize=PictureWidth-matrixSize;
            int HeightMMatrixSize=PictureHeight-matrixSize;
            int Stride=dataNew.Stride;  // Stride = PictureWidth*3

            // Prohledá pixely okolo, pokuď je vnitřní hodně velký nebo malý oproti krajním, tak zarovnej.
            Parallel.For(matrixSize, HeightMMatrixSize, (y, state) => {
                byte* row=fromW+y*Stride;

                for (int x=matrixSize; x<WidthMMatrixSize; x++) {

                    // Získa krajní pixely
                    int[,] values=new int[3, 3];
                    for (int yy=0; yy<3; yy++) {
                        byte* MRow=fromR+(y+yy-1)*Stride;
                        for (int xx=0; xx<3; xx++) {
                            values[xx, yy] = *(MRow+(x+xx-1)*3);
                        }
                    }
                 
                    // Prostřední pixel
                    int centre=values[1, 1];

                    // Prostření barva má znatelný výstřel
                    // Centre is lower
                    if (values[1, 0]>centre && values[0, 1]>centre && values[1, 2]>centre && values[2, 1]>centre)  
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    
                    // Centre is bigger
                    else if (values[1, 0]<centre && values[0, 1]<centre && values[1, 2]<centre && values[2, 1]<centre) 
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    
                    // Centre is lower
                    else if (values[1, 0]>centre && values[0, 1]>centre && values[1, 2]>centre && values[2, 1]<centre) 
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    else if (values[1, 0]>centre && values[0, 1]>centre && values[1, 2]<centre && values[2, 1]>centre) 
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    else if (values[1, 0]>centre && values[0, 1]<centre && values[1, 2]>centre && values[2, 1]>centre) 
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    else  if (values[1, 0]<centre && values[0, 1]>centre && values[1, 2]>centre && values[2, 1]>centre)
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    else

                    // Centre is bigger
                    if (values[1, 0]<centre && values[0, 1]<centre && values[1, 2]<centre && values[2, 1]>centre) 
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    else if (values[1, 0]<centre && values[0, 1]<centre && values[1, 2]>centre && values[2, 1]<centre) 
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    else if (values[1, 0]<centre && values[0, 1]>centre && values[1, 2]<centre && values[2, 1]<centre) 
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                    else if (values[1, 0]>centre && values[0, 1]<centre && values[1, 2]<centre && values[2, 1]<centre) 
                        SetPixelNew(x, y, (byte)((values[1, 0]+values[0, 1]+values[1, 2]+values[2, 1])/4), row);
                }
            });

            // Nastav pixelu barvu 
            void SetPixelNew(int x, int y, byte b, byte* row) {
                byte* head=row+x*3;
                *head = b;
                *(head+1) = b;
                *(head+2) = b;
            }

            bitmapNew.UnlockBits(dataNew);
            DA.SetData(0, bitmapNew);

            bitmap.UnlockBits(dataOriginal);
        }

        protected override Bitmap Icon => Properties.Resources.clean;

        public override Guid ComponentGuid => new Guid("49D8A02A-37C5-4FCA-9839-799AA04BFD56");
    }
}