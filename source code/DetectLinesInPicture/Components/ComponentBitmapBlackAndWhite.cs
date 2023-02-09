using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    public class ComponentToBlackAndWhite : GH_Component {
        public ComponentToBlackAndWhite()
          : base("Black And White", "Black And White",
            "Black And White",
            "Detector", "Modify image") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Barevný obrázek\nBitmap colorful image", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Černobílý obrázek\nBitmap graystyle image", GH_ParamAccess.item);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            Bitmap bitmap=null;

            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }

            Rectangle recSize=new Rectangle(0,0, bitmap.Width, bitmap.Height);

            Bitmap bitmapNew=(Bitmap)bitmap.Clone();

            BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            byte* fromW=(byte*)dataNew.Scan0;
            int Width=recSize.Width;
            int Stride=dataNew.Stride;
            int PixelWidth=recSize.Width*3;

            Parallel.For(0, recSize.Height, (y, state) => {
                byte* row=fromW+y*Stride;
                for (int x=0; x<PixelWidth; x+=3) {
                    byte* headR=row+x;
                    byte value=(byte)((*headR+*(headR+1)+*(headR+2))/3f);

                    byte* headW=row+x;
                    *headW = value;
                    *(headW+1) = value;
                    *(headW+2) = value;
                }
            });

            bitmapNew.UnlockBits(dataNew);
            DA.SetData(0, bitmapNew);
        }

        protected override Bitmap Icon => Properties.Resources.gray;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C5-4FCA-9839-799AA04BFD57");
    }
}