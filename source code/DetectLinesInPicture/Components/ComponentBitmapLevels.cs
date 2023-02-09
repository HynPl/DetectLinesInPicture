using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    public class ComponentLevels : GH_Component {
        public ComponentLevels()
          : base("Gray level", "Composity",
            "Úrovně šedé\nGray level",
            "Detector", "Modify image") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap",      "Bitmap",   "černobílý obrázek\nBitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter ("Gray min",    "Number",   "Minimální šedá\nMin of gray", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter ("Gray max",    "Number",   "Maximální šedá\nMax of gray", GH_ParamAccess.item, 255.0);
            pManager.AddBooleanParameter("Reverse out", "Boolean",  "Obrátit min a max\nReverse", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {

            // Získat vstup
            Bitmap bitmap=null;
            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }

            double rawGrayLevelMin=0d;
            DA.GetData(1, ref rawGrayLevelMin);
            int GrayLevelMin=(int)rawGrayLevelMin;
            
            double rawGrayLevelMax=255d;
            DA.GetData(2, ref rawGrayLevelMax);
            int GrayLevelMax=(int)rawGrayLevelMax;

            bool rawRevOut=true;
            DA.GetData(3, ref rawRevOut);
            bool reverseOut=rawRevOut;

            int PictureWidth=bitmap.Width;
            int PictureHeight=bitmap.Height;

            Rectangle recSize=new Rectangle(0,0, PictureWidth, PictureHeight);
            Bitmap bitmapNew=bitmap.Clone(recSize, PixelFormat.Format24bppRgb);

            BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            byte* fromW=(byte*)dataNew.Scan0;

            int Stride=dataNew.Stride;

            if (reverseOut) {
                Parallel.For(0, PictureHeight, (y, state) => {
                    byte* rowW=fromW+y*Stride;

                    for (int x=0; x<Stride; x+=3) {   
                        byte* headR=rowW+x;
                        int sat=GetSaturation(*headR, *(headR+1), *(headR+2));
                    
                        // Zaokrouhlení nahoru/dolů pixelů
                        if (sat<GrayLevelMin) {
                            byte* headW=rowW+x;
                            *headW     = 0;
                            *(headW+1) = 0;
                            *(headW+2) = 0;
                        } else if (sat>GrayLevelMax) {
                            byte* headW=rowW+x;
                            *headW     = 255;
                            *(headW+1) = 255;
                            *(headW+2) = 255;
                        }
                    }
                });
            } else {
                Parallel.For(0, PictureHeight, (y, state) => {
                    byte* rowW=fromW+y*Stride;

                    for (int x=0; x<Stride; x+=3) {   
                        byte* headR=rowW+x;
                        int sat=GetSaturation(*headR, *(headR+1), *(headR+2));

                        // Zaokrouhlení nahoru/dolů pixelů
                        if (sat<GrayLevelMin) {
                            byte* headW=rowW+x;
                            *headW     = 255;
                            *(headW+1) = 255;
                            *(headW+2) = 255;
                        } else if (sat>GrayLevelMax) {
                            byte* headW=rowW+x;
                            *headW     = 0;
                            *(headW+1) = 0;
                            *(headW+2) = 0;
                        }
                    }
                });
            }
            bitmapNew.UnlockBits(dataNew);

            DA.SetData(0, bitmapNew);
        }            
        
        // Možná rychlejší implementace (int)(System.Drawing.Color.GetSaturation()*255)
        static int GetSaturation(int r, int g, int b) { 
            int cmax, cmin;
            // r > b > g || g > r > b 
            if (r>g) { 
                // min : g or b
                // max : r or b

                if (g>b) { 
                    // min : b
                    // max : r or b
                    cmin=b;
                    if (r>b) cmax=r; else cmax=b;
                } else { 
                    // min : g
                    // max : r or b
                    cmin=g;
                    if (r>b) cmax=r; else cmax=b;
                }
            }else{ //r<g
                // min : r or b
                // max : g or b

                if (r>b) {
                    // min : b
                    // max : g or b
                    cmin=r;
                    if (g>b) cmax=g; else cmax=b;
                } else { 
                    // min : r
                    // max : g or b
                    cmin=r;
                    if (g>b) cmax=g; else cmax=b;
                }
            }

            // Proti dělení nulou
            if (cmax == 0) return 0;
                
            // Výpočet saturování
            //return (int)((float)(cmax - cmin) / cmax * 255f);
            return 255 - (int)(255*(float)cmin/cmax);
        }

        protected override Bitmap Icon => Properties.Resources.graylevel;

        public override Guid ComponentGuid => new Guid("49D8A02B-30C5-4FAA-9849-799AA04BFD57");
    }
}