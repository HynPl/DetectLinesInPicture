using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    public class ComponentConposity : GH_Component {
        public ComponentConposity()
          : base("Composity", "Composity",
            "Nastavení jasu, kontrastu, ... na základě černobílého obrázku",
            "Detector", "Modify image") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap",          "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter ("Contrast",        "Number", "Kontrast obrázku (kolem 1)\nContrast of image (around 1)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter ("Brightness",      "Number", "Jas obrázku (-255 až 255)\nLight of image (from -255 to 255)", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter ("Gama",            "Number", "Gama obrázku (kolem 1)\nTreeshold of ignore gray (around 1)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter ("ThresholdUp",     "Number", "Zaokrouhlit pixely nahoru k bílé (0 až 255)\nTreeshold for high pixels (from 0 to 255)", GH_ParamAccess.item, 255.0);
            pManager.AddNumberParameter ("ThresholdDown",   "Number", "Zaokrouhlit pixely dolů k černé (0 až 255)\nTreeshold for low pixels (from 0 to 255)", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Výstupní obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            // Získat vstup
            Bitmap bitmap=null;
            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }

            double rawContrast=1d;
            DA.GetData(1, ref rawContrast);
            float Contrast=(float)rawContrast;

            double rawBrightness=1d;
            DA.GetData(2, ref rawBrightness);
            float Brightness=(float)rawBrightness;

            double rawGama=1d;
            DA.GetData(3, ref rawGama);
            float Gama=(float)rawGama;

            double rawThresholdUp=255d;
            DA.GetData(4, ref rawThresholdUp);
            int ThresholdUp=(int)rawThresholdUp;

            double rawThresholdDown=0d;
            DA.GetData(5, ref rawThresholdDown);
            int ThresholdDown=(int)rawThresholdDown;


            int PictureWidth=bitmap.Width;
            int PictureHeight=bitmap.Height;

            Rectangle recSize=new Rectangle(0,0, PictureWidth, PictureHeight);

            // Výsledný obrázek bude tady
            Bitmap bitmapNew=new Bitmap(PictureWidth, PictureHeight);

            BitmapData dataOriginal=bitmap.LockBits(recSize, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            byte* fromW=(byte*)dataNew.Scan0, fromR=(byte*)dataOriginal.Scan0;

            int Stride=dataOriginal.Stride;

            // Parallel.For je něco jako for (int i=0; i<PictureHeight; i++){ ... } akorát rozhodí vnitřek nezávisle do jader CPU (samozdřejmě GPU to zvládne ještě rychléš...)
            Parallel.For(0, PictureHeight, (y, state) => {
                int line=y*Stride;
                byte* rowR=fromR+line;
                byte* rowW=fromW+line;

                // Po pixelových chanelech
                for (int x=0; x<Stride; x++) {
                    // Jas a gama
                    int valueN=(int)((*(rowR+x)+Brightness)*Gama+0.5f);

                    // Kontrast
                    if (valueN<127) valueN=127-(int)((127-valueN)*Contrast);
                    else if (valueN>127) valueN=127+(int)((valueN-127)*Contrast);

                    byte value;

                    // Thresholds
                    if (valueN<ThresholdDown) value=0;
                    else if (valueN>ThresholdUp) value=255;
                    else value=(byte)valueN;

                    // Zápis pixelu
                    *(rowW+x) = value;
                }
            });

            bitmap.UnlockBits(dataOriginal);
            bitmapNew.UnlockBits(dataNew);

            DA.SetData(0, bitmapNew);
        }

        protected override Bitmap Icon => Properties.Resources.contrast;

        public override Guid ComponentGuid => new Guid("49D8A02B-30C5-4FCA-9839-799AA04BFD57");
    }
}