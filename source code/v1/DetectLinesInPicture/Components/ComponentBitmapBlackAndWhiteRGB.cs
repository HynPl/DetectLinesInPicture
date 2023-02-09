using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    public class ComponentToBlackAndWhiteRGB : GH_Component {
        public ComponentToBlackAndWhiteRGB()
          : base("Black And White RGB", "Black And White RGB",
            "Tato komponenta převede obrázek do černobílé podoby, můžete zadat poměr barev (RGB) z které má vzniknout černobílý odstín",
            "Detector", "Modify image") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Bitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Red",   "Number", "-1 to 1", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Green", "Number", "-1 to 1", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Blue",  "Number", "-1 to 1", GH_ParamAccess.item, 1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Bitmap image", GH_ParamAccess.item);
        }
   
      //  [Alea.GpuManaged]
        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            Bitmap bitmap=null;
            double 
                rawRed=1.0, 
                rawGreen=1.0, 
                rawBlue=1.0;

            // Načti
            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }
            DA.GetData(1, ref rawRed);
            DA.GetData(2, ref rawGreen);
            DA.GetData(3, ref rawBlue);

            // Převeď na float abe vépočet bel rychléši
            float red=(float)rawRed,
                green=(float)rawGreen,
                blue=(float)rawBlue,

            total=red+green+blue;

            // Obečéňě (gdež plati r=b=g=1): (r+g+b)/3 = r/3+g/3+b/3
            red/=total;
            green/=total;
            blue/=total;

            int PictureWidth=bitmap.Width;
            int PictureHeight=bitmap.Height;

            Rectangle recSize=new Rectangle(0,0, PictureWidth, PictureHeight);

            Bitmap bitmapNew=new Bitmap(PictureWidth, PictureHeight);

            BitmapData dataOriginal=bitmap.LockBits(recSize, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dataNew=bitmapNew.LockBits(recSize, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            byte* fromW=(byte*)dataNew.Scan0, fromO=(byte*)dataOriginal.Scan0;

            int Stride=dataOriginal.Stride;

            //Gpu.Default.For(0, PictureHeight, y =>{
            Parallel.For(0, PictureHeight, (y, state) => {
                int line=y*Stride;
                byte* rowR=fromO+line;
                byte* rowW=fromW+line;

                for (int x=0; x<Stride; x+=3) {                    
                    byte* headR=rowR+x,
                          headG=headR+1,
                          headB=headR+2;

                    // Hodnota barve, kerá se bode zapisovat 
                    float valueN=red**headR+green**headG+blue**headB;
                    byte value=valueN>255f ? value=255 : valueN<0f ? value=0 : value=(byte)valueN;
             
                    // Write bytes
                    byte* headW=rowW+x;
                    *headW     = value;
                    *(headW+1) = value;
                    *(headW+2) = value;
                }
            });

            bitmap.UnlockBits(dataOriginal);
            bitmapNew.UnlockBits(dataNew);

            DA.SetData(0, bitmapNew);
        }

     
        //private void RunGPU(){ 
        //      Parallel.For(0, PictureHeight, (y, state) => {
        //        int line=y*Stride;
        //        byte* rowR=fromO+line;
        //        byte* rowW=fromW+line;

        //        for (int x=0; x<Stride; x+=3) {                    
        //            byte* headR=rowR+x,
        //                  headG=headR+1,
        //                  headB=headR+2;

        //            // Hodnota barve, kerá se bode zapisovat 
        //            float valueN=red**headR+green**headG+blue**headB;
        //            byte value=valueN>255f ? value=255 : valueN<0f ? value=0 : value=(byte)valueN;

        //            // Write bytes
        //            byte* headW=rowW+x;
        //            *headW = value;
        //            *(headW+1) = value;
        //            *(headW+2) = value;
        //        }
        //    });
        //}

        protected override Bitmap Icon => Properties.Resources.grayrgb;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C5-4FCA-9839-799AA04BED57");
    }
}