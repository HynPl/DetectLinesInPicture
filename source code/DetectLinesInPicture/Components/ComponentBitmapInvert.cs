using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Threading.Tasks;

namespace DetectLinesInPicture {

    public class ComponentInvertColor : GH_Component {

        public ComponentInvertColor()
          : base("Invert Color", "Invert Color",
            "Invertování obrázku\nInvert Color",
            "Detector", "Modify image") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
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

            // Rozměr obrázku
            int PictureWidth=bitmap.Width, PictureHeight=bitmap.Height;
            Rectangle recSize=new Rectangle(0,0, PictureWidth, PictureHeight);
            
            BitmapData dataOriginal=bitmap.LockBits(recSize, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            Bitmap bitmapNew=new Bitmap(PictureWidth, PictureHeight);
            BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            // Začátek pole pixelů
            byte* fromW=(byte*)dataNew.Scan0, 
                  fromR=(byte*)dataOriginal.Scan0;

            int Stride=dataOriginal.Stride;

            // Projít po pixelech, nezávisle řádky
            Parallel.For(0, PictureHeight, (y, state) => {
                int line = y * Stride;
                byte* rowR = fromR + line,
                      rowW = fromW + line;

                // barva pixelu=255-barva pixelu
                for (int x = 0; x<Stride; x++) *(rowW+x) = (byte)(255 - *(rowR+x));
            });

            bitmapNew.UnlockBits(dataNew);

            // Odeskat výstup
            DA.SetData(0, bitmapNew);

            bitmap.UnlockBits(dataOriginal);
        }

        protected override Bitmap Icon => Properties.Resources.invert;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C5-4FCA-9839-799AA04BFD54");
    }
}