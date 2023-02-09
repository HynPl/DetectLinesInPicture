using Grasshopper.Kernel;
using Rhino.Render;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    public class ComponentBlur : GH_Component {
        public ComponentBlur()
          : base("Blur", "Blur",
            "Gaussovské rozostření",
            "Detector", "Modify image") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "černobílý obrázek\nBitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Size", "Number", "Velikost rozostření\nSize of blur", GH_ParamAccess.item, 3);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            // Vstup
            Bitmap bitmap=null;
            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }

            double RawNumber=3;
            DA.GetData(1, ref RawNumber);
            int BlurSize=(int)RawNumber;
            if (BlurSize<0)BlurSize=0;
            
            // Žádné rozmazání
            Bitmap bitmapNew=(Bitmap)bitmap.Clone();
            if (BlurSize==0) { 
                DA.SetData(0, bitmapNew);
                return;
            }

            double sigma=1.0;
            int matrixLen = 1 + BlurSize*2; // délka matice = BlurSize + 1 + BlurSize
            float[] blurArr=new float[matrixLen];
           
            {
                double r, s = 2.0 * sigma * sigma;
                float sum = 0f;
                for (int x = 0; x < matrixLen; x++) {
                    float xx=x-BlurSize;
                    r = Math.Sqrt(xx * xx);
                    blurArr[x] = (float)(Math.Pow(-(r*r)/s, 2) / (Math.PI*s));
                }

                // Převrátit hodnoty
                float rev=blurArr[0];
                for (int x = 0; x < matrixLen; ++x) {                    
                    blurArr[x] = rev-blurArr[x];
                    sum += blurArr[x];
                }
  
                // Normalizace, tak aby součet hodnot v poli byl 1
                for (int x = 0; x < matrixLen; ++x) blurArr[x]/=sum;
            }

            int BitmapWidth=bitmap.Width;
            int BitmapHeight=bitmap.Height;
            Rectangle recSize=new Rectangle(0,0, BitmapWidth, BitmapHeight);

            BitmapData dataOriginal=bitmap.LockBits(recSize, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            // Dva průchody horizontálně a vertikálně
            // Dělení na kraj a střed, ve středu se nemusí řešit vylezení z obrázku, u krajní uzpůsob nedodělek za hranou 
            // Vertikálně
            {
                BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                int Stride=dataNew.Stride;
                byte* fromW=(byte*)dataNew.Scan0;
                byte* fromR=(byte*)dataOriginal.Scan0;
              
                int BitmapWidth3=BitmapWidth*3;

                // Krajní
                Parallel.For(0, BlurSize, (y, state) => {
                    byte* rowW=fromW+y*Stride;

                    for (int x3=0; x3<BlurSize*3; x3+=3) {
                        float forceTotal=0;
                        float sum=0;

                        // Výpočet pixelu
                        for (int my=0; my<matrixLen; my++) {
                            int yy=y+(my-BlurSize);
                       
                            if (yy<0) continue;
                            if (yy>=BitmapHeight) continue;

                            forceTotal+=(*(fromR+yy*Stride+x3))*blurArr[my];
                            sum+=blurArr[my];
                        }

                        // Zápis pixelu
                        *(rowW+x3) = (byte)(0.5f + forceTotal/sum);
                    }   
                });

                // Prostřední
                Parallel.For(BlurSize, BitmapHeight-BlurSize, (y, state) => {   
                    byte* rowW=fromW+y*Stride;
                    int ymatrixLen2=y-BlurSize;

                    for (int x3=BlurSize*3; x3<BitmapWidth3-BlurSize*3; x3+=3) {
                        float forceTotal=0;

                        // Výpočet pixelu
                        for (int my=0; my<matrixLen; my++) {
                            forceTotal+=(*(fromR+(my+ymatrixLen2)*Stride+x3))*blurArr[my];
                        }

                        // Zápis pixelu
                        *(rowW+x3) = (byte)(forceTotal + 0.5f);
                    }  
                });

                // Krajní
                Parallel.For(BitmapHeight-BlurSize, BitmapHeight, (y, state) => {
                    byte* rowW=fromW+y*Stride;

                    for (int x3=BlurSize*3; x3<BitmapWidth3; x3+=3) {
                        float forceTotal=0;
                        float sum=0;

                        // Výpočet pixelu
                        for (int my=0; my<matrixLen; my++) {
                            int yy=y+my-BlurSize;
                       
                            if (yy<0) continue;
                            if (yy>=BitmapHeight) continue;

                            sum+=blurArr[my];
                            forceTotal+=(*(fromR+yy*Stride+x3))*blurArr[my];
                        }

                        // Zápis pixelu
                        *(rowW+x3) = (byte)(forceTotal/sum + 0.5f);
                    }  
                });
                bitmapNew.UnlockBits(dataNew);
                bitmap.UnlockBits(dataOriginal);
            }

            // Horizontálně
            {
                BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); // chanels are RGBA
                byte* fromW=(byte*)dataNew.Scan0;
                int Stride=dataNew.Stride;
                                
                Parallel.For(0, BitmapHeight, (y, state) => {
                    byte* row=fromW+y*Stride;
              
                    // Krajní
                    for (int x=0; x<BlurSize; x++) {
                        float forceTotal=0;
                        float sum=0;
                        int xmatrixLen2=x-BlurSize;

                        // Výpočet pixelu
                        for (int mx=0; mx<matrixLen; mx++) {
                            int xx=mx-xmatrixLen2;
                            if (xx<0) continue;
                            if (xx>=BitmapWidth) continue;
                      
                            forceTotal+=(*(row+xx*3))*blurArr[mx];
                            sum+=blurArr[mx];
                        }

                        // Zápis pixelu
                        byte value=(byte)(forceTotal/sum + 0.5f);
                        byte* head=row+x*3;
                        *head = value;
                        *(head+1) = value;
                        *(head+2) = value;
                    }

                    // Prostřední
                    for (int x=BlurSize; x<BitmapWidth-BlurSize; x++) {
                        float forceTotal=0;
                        int xmatrixLen2=x-BlurSize;

                        // Výpočet pixelu
                        for (int mx=0; mx<matrixLen; mx++) {
                            forceTotal+=(*(row+(mx+xmatrixLen2)*3))*blurArr[mx];
                        }

                        // Zápis pixelu
                        byte value=(byte)(forceTotal + 0.5f);
                        byte* head=row+x*3;
                        *head = value;
                        *(head+1) = value;
                        *(head+2) = value;
                    }

                    // Krajní
                    for (int x=BitmapWidth-BlurSize; x<BitmapWidth; x++) {
                        float forceTotal=0;
                        float sum=0;
                        int xmatrixLen2=x+BlurSize;

                        // Výpočet pixelu
                        for (int mx=0; mx<matrixLen; mx++) {
                            int xx=mx-xmatrixLen2;
                            if (xx<0) continue;
                            if (xx>=BitmapWidth) continue;
                      
                            forceTotal+=(*(row+xx*3))*blurArr[mx];
                             sum+=blurArr[mx];
                        }

                        // Zápis pixelu
                        byte value=(byte)(forceTotal/sum + 0.5f);
                        byte* head=row+x*3;
                        *head = value;
                        *(head+1) = value;
                        *(head+2) = value;
                    }
                });

                bitmapNew.UnlockBits(dataNew);
            }

            DA.SetData(0, bitmapNew);
        }

        protected override Bitmap Icon => Properties.Resources.blur;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C5-4FCA-9839-799AA04BED56");
    }
}