using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DetectLinesInPicture {
    public class ComponentGetPixel : GH_Component {
        public ComponentGetPixel()
          : base("Get pixel color", "Get pixel color",
            "Získání barvy pixelů\nGet pixel color",
            "Detector", "Basic") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
            pManager.AddGenericParameter("Point", "Position", "Pozice\nPosition of pixel (xy point)", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddColourParameter("Colour", "Colour", "Barvy pixelu\nColour of pixel", GH_ParamAccess.list);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            // vstup
            Bitmap bitmap=null;
            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }

            List<Point3d> points=new List<Point3d>();
            if (!DA.GetDataList(1, points)) { 
                DA.AbortComponentSolution();
                return;
            }
            
            // Metoda 1, Nevím kolik to přesně je aby se to vyplatilo tak jsem plácnul 5
            if (points.Count<5) { 
                int len=points.Count;
                GH_Colour[] colors=new GH_Colour[len];

                for (int i=0; i<len; i++) {
                    Point3d point=points[i];
                    colors[i]=new GH_Colour(bitmap.GetPixel((int)point.X,(int)point.Y));
                }

                DA.SetDataList(0, colors);

            // Metoda 2, pro hodně pixelů
            } else {
                int w=bitmap.Width, h=bitmap.Height, len=points.Count;
                Rectangle rec=new Rectangle(0,0,w,h);
                GH_Colour[] colors=new GH_Colour[len];
            
                BitmapData bitmapData=bitmap.LockBits(rec, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                int* from=(int*)bitmapData.Scan0;
            
                ParallelLoopResult result = Parallel.For(0, len, (x, state) => {
                    Point3d point=points[x];
                    colors[x]=new GH_Colour(Color.FromArgb(*(from+(((int)point.Y)*w+(int)point.X))));
                });

                bitmap.UnlockBits(bitmapData);
                DA.SetDataList(0, colors);
            }
        }

        protected override Bitmap Icon => Properties.Resources.get_pixel;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C5-4FCA-9839-798AA04BFD55");
    }
}