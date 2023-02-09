using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace DetectLinesInPicture {
    public class ComponentDetectLines : GH_Component {
        public ComponentDetectLines() : base("Detect Lines", "Lines detection",
            "Description",
            "Detector", "Detecting") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap",                  "Bitmap", "Obr·zek ve tvaru Ëern·=pozadÌ, bÌl·=Ë·ry\nBitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Mode",                     "Mode",   "P¯edbÏûnÈ hlen·nÌ Ëar (od 0 do 1, vÏtöÌ ËÌslo lepöÌ)\nPresearch, 0 fast and unprecise to 1 better quality", GH_ParamAccess.item, 0.95);
            pManager.AddNumberParameter("Thickness",                "Number", "Tlouöùka Ëar\nLine thickness", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Connect others",           "Number", "P¯ipojenÌ k ostatnÌm Ëar·m\nConnect other polylines, bigger number connect more far ones", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Closing self",             "Number", "UzavÌr·nÌ vlatnÌ polyline\nConnect start of specific polyline to his end, bigger number connect more far ones", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Connect to body",          "Number", "P¯ipojenÌ k tÏlu polyline (VhodnÈ na tvary typu '8')\nConnect other polylines body, bigger number connect more far ones", GH_ParamAccess.item, 4);
            pManager.AddNumberParameter("Simplify",                 "Number", "Zjednoduöit v˝stup (0=nezjednoduöovat)\nSimplify polyline, begger more simplify, 0=no symplify. RamerñDouglasñPeucker algorithm - eta value", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Ignore pixels presearch",  "Number", "Ingnorovat nÌzkÈ pixely v p¯edbÏûnÈm hlen·nÌ (0=neignorovat)\nPresearch ignore low pixel value (1=no ignore .. to .. 255=max ignore)", GH_ParamAccess.item, 50);
            pManager.AddNumberParameter("Ignore pixels",            "Number", "Ingnorovat nÌzkÈ pixely (0=neignorovat)\nIgnore low pixel value (1=no ignore .. to .. 255=max ignore)", GH_ParamAccess.item, 5);
            pManager.AddBooleanParameter("Only closed",             "Boolean", "Pouze urav¯enÈ polyline\nGives only closed polylines", GH_ParamAccess.item, false);

            // Pozice a velikost
            pManager.AddPointParameter("Point", "Position", "Pozice umÌstÏnÌ\nPosition", GH_ParamAccess.item, new Point3d(0,0,0));
            pManager.AddVectorParameter("Vector", "Scale", "velikost v˝stupu, z·porn· znamen· p¯evr·tit\nScale, flip, (z - don't care)", GH_ParamAccess.item, new Vector3d(1,1,0));

        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Curves", "Curves", "K¯ivky\nCurves (curve can be line)", GH_ParamAccess.list);  
            #if DEBUG
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obr·zek\nBitmap image", GH_ParamAccess.item);
            #endif
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {

            // Set up varibles
            Bitmap bitmapOriginal = null;
            double RawTickness = 3.0;
            double quality=0.5;
            double RawConnectToOthers=1.0;
            double RawConnectToSelf=1.0;
            double RawConnectToBody=1.0;
            double RawSimplify=1.0;
            double RawIgnore=50;
            double RawIgnore2=5;
            bool ClosedOnly=false;
            Vector3d TransformVector = new Vector3d(1,1,0);
            Point3d TransformLocation = new Point3d(0,0,0);
            int tickness;

            // Load inputs
            if (!DA.GetData(0, ref bitmapOriginal)) {
                DA.AbortComponentSolution();
                return;
            } 
            
            int PictureWidth=bitmapOriginal.Width;
            int PictureHeight=bitmapOriginal.Height;  
            Rectangle rec = new Rectangle(0, 0, PictureWidth, PictureHeight);
            Bitmap bitmap = bitmapOriginal.Clone(rec, PixelFormat.Format24bppRgb);

            if (DA.GetData(2, ref RawTickness)) {
                if (RawTickness<=0) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,"Tickness should be bigger than zero");
                    DA.AbortComponentSolution();
                    return;
                }
            }
            tickness=(int)(RawTickness+0.5f);

            // Self close
            DA.GetData(3, ref RawConnectToOthers);
            float connectToOthers=(float)RawConnectToOthers;


            DA.GetData(4, ref RawConnectToSelf);
            float connectConnectToSelf=(float)RawConnectToSelf;

            DA.GetData(5, ref RawConnectToBody);
            float connectToBody=(float)RawConnectToBody; 
            
            DA.GetData(6, ref RawSimplify);
            float simplify=(float)RawSimplify;

            DA.GetData(7, ref RawIgnore);
            int ignore=(int)RawIgnore;
            if (ignore<1)ignore=1;
            if (ignore>255)ignore=255;

            DA.GetData(8, ref RawIgnore2);
            int ignore2=(int)RawIgnore2;
            if (ignore2<1)ignore2=1;
            if (ignore2>255)ignore2=255;
            DA.GetData(9, ref ClosedOnly);

            DA.GetData(10, ref TransformLocation);
            DA.GetData(11, ref TransformVector);


            PackMan.DistanceMin=tickness;
            PackMan.DistanceMax=tickness+1;

            PackMan.DistanceMinD=PackMan.DistanceMin;
            PackMan.DistanceMaxD=PackMan.DistanceMax;

            if (!DA.GetData(1, ref quality)) {
                quality=0.5;
            }
           
            PackMan.PictureHeight=PictureHeight;
            PackMan.PictureWidth=PictureWidth;
                      
            BitmapData data = bitmap.LockBits(rec, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            byte* pointer = (byte*)data.Scan0;
            PackMan.Pointer=pointer;
            PackMan.Stride=data.Stride;

            // Clean image, one chanel for source, one for already solved
            {
                byte* pointerClean=pointer+1;
                int to=rec.Width*rec.Height*3;
                byte* pointerCleanTo=pointerClean+to;
                for (; pointerClean<pointerCleanTo; pointerClean+=3) *pointerClean = 0;

                // useless, maybe debug only
                pointerClean=pointer+2;
                for (; pointerClean<pointerCleanTo; pointerClean+=3) *pointerClean = 0;
            }

            // Analyze image
            Analyzator.PictureWidth=PictureWidth;
            Analyzator.PictureHeight=PictureHeight;
            Analyzator.Stride=data.Stride;
            Analyzator.Pointer=pointer;
            int min=1;
            int max;
            if (PictureWidth<PictureHeight) max=bitmap.Height/2;
            else max=bitmap.Width/2;

            Analyzator.NextPoint=(int)(min+(max-min)*(1-quality));
            List<DInt> PreAnalyzed=Analyzator.Analyze(ignore);
           
            float limitDistanceToConnectToOthers =tickness*connectToOthers ;
            float limitDistanceToConnectToSelf=tickness*connectConnectToSelf;
            float limitDistanceToConnectToBody=tickness*connectToBody;

            List<MyPolyline> polylines=new List<MyPolyline>();
            foreach (DInt pt in PreAnalyzed) {
                
                // Ze st¯edo na levo a na pravo  <<-<X>->>
                MyPolyline 
                    left=RunPackMann(), 
                    right=RunPackMann();

                MyPolyline RunPackMann(){
                    PackMan packMan = new PackMan(pt.X, pt.Y);

                    List<DIntB> Points = new List<DIntB>();
                    for (int i = 0; i < 1000; i++) {

                        // Gde be ses muhls p¯esonÛt
                        TInt item = packMan.FindNextPoint(i==0);

                        // NÈni gde se p¯esonÛt
                        if (item.X == -1) {
                            break;
                        }

                        // NemÏlo by nastat, ale co uû vezkÛöÈ
                        if (item.X==packMan.currentPosX && item.Y==packMan.currentPosY) {

                            // Zkus odÏlat smeËko
                            if (Points.Count > 7 && Points.Count>(int)limitDistanceToConnectToSelf+1) {
                                DIntB start = Points[0];
                                int dX = start.X - item.X, dY = start.Y - item.Y;
                                double dis = Math.Sqrt(dX*dX + dY*dY);
                                if (dis < limitDistanceToConnectToSelf/3) {
                                    if (Points[0].X==item.X && Points[0].Y==item.Y){ 
                                        Points[0].SetN(true);
                                        Points[Points.Count-1].SetN(true);
                                    }else{
                                        Points.Add(new DIntB(start.X, start.Y, true));
                                        Points[0].SetN(true);
                                    }
                                    break;
                                }
                            }
                            break;
                        }

                        if (item.N>0 && item.N<ignore2) break;

                        // Packmann: Tady jsem uû byl, dalöÌ to tady ¯eöit nebudou 
                        // ZaplÚ aktu·lni pozeco
                        /*if (Points.Count>0)*/PackMan.SetValueArea(packMan.currentPosX, packMan.currentPosY, tickness-1, 1);

                        // packMan.SetValueArea(item.X, item.Y, (int)(tickness-1/**connect*/), 1);
                       
                        if (Points.Count > 2) {
                            DIntB last=Points[Points.Count - 1];
                            DIntB prevLast=Points[Points.Count - 2];
                            int x = (last.X + prevLast.X) / 2;
                            int y = (last.Y + prevLast.Y) / 2;
                            PackMan.SetValueArea(x, y, tickness/*+1*/, 1);
                        }
                                                
                        if (Points.Count==3) { 
                            PackMan.SetValueArea2(Points[0].X, Points[0].Y, tickness, 2);
                            PackMan.SetValueArea(Points[1].X, Points[1].Y, tickness, 0);

                            DIntB last=Points[0];
                            DIntB prevLast=Points[1];
                            int x = (last.X + prevLast.X) / 2;
                            int y = (last.Y + prevLast.Y) / 2;
                            PackMan.SetValueArea(x, y, tickness/*+1*/, 1);
                        }

                        if (Points.Count > 7 && Points.Count>(int)limitDistanceToConnectToSelf+1) {
                            DIntB start = Points[0];
                            int dX = start.X - item.X, dY = start.Y - item.Y;

                            double dis = Math.Sqrt(dX*dX + dY*dY);
                            if (dis < limitDistanceToConnectToSelf/3) {
                                Points.Add(new DIntB(start.X, start.Y, true));
                                Points[0].SetN(true);
                                break;
                            }
                        }

                        // Spojit zaË·tek a konec
                        if (item.N==-2) { 
                            PackMan.SetValueArea2(Points[0].X, Points[0].Y, tickness, 1);
                            Points.Add(new DIntB(Points[0].X, Points[0].Y,true));
                            Points[0].SetN(true);
                            break;
                        }

                        // P¯esoÚ packmanna na novÛ pozeco
                        Points.Add(new DIntB(item.X, item.Y,false));
                        packMan.SetPosition(item.X, item.Y);
                    }
                
                    // SamotÈ bod nem· ani ceno ¯eöet
                    if (Points.Count == 0) return null;
                    if (Points.Count == 1) { 
                        DIntB p=Points[0];
                    //    PackMan.SetValueArea(p.X, p.Y, tickness+1, 0);
                        return null;
                    }

                 //   PackMan.SetValueArea(Points[0].X, Points[0].Y, tickness-1, 0);
                    PackMan.SetValueArea2(Points[0].X, Points[0].Y, tickness, 1);
                    MyPolyline polyline=new MyPolyline(Points/*, false, false*/);
                    return polyline;
                }
                
                // Propoj na pravo a na levo  E<<<<SXS>>>>E  =>   S>>>>X>>>>E
                if (left!=null && right!=null) { 
                    //if (left.Points[0].X==right.Points[0].X && left.Points[0].Y==right.Points[0].Y)
                    //    continue;
                    if (left.TryMergeWith(right)) { 
                        polylines.Add(left);
                        PackMan.SetValueArea(left.Points[0].X, left.Points[0].Y, tickness+1, 1);
                        PackMan.SetValueArea(right.Points[0].X, right.Points[0].Y, tickness+1, 1);
                    } else { 
                        // Add polylines to list
                        polylines.Add(left);
                        polylines.Add(right);
                    }                    
                } else { 
                    MyPolyline polyline;

                    if ((polyline=left!=null ? polyline=left : (right!=null) ? polyline=right : null) != null) {
                        // Try close self
                        if (!polyline.TryCloseSelf(limitDistanceToConnectToSelf, (int)connectConnectToSelf+2)) PackMan.SetValueArea(polyline.Points[0].X, polyline.Points[0].Y, tickness, 1);
                        
                        // Add polyline to list
                        polylines.Add(polyline);
                    } 
                }
            }            
            
           

            // Connect polylines
            // Zkus napojit na jinou smyËku jak na vlastni
            foreach (MyPolyline pFrom in polylines) {                 
                if (pFrom.Start && pFrom.End) continue;

                DIntB end=pFrom.Points[pFrom.Points.Count-1];

                int nearestS=-1,
                    nearestE=-1;

                double 
                    disEndToStart=0,
                    disEndToEnd=0;                    

                for (int j=0; j<polylines.Count; j++) {
                    MyPolyline pTo = polylines[j];

                    // End to start
                    if (!pTo.Start) {
                        // try connect to starting point
                        DIntB Tstart = pTo.Points[0];
                        int dX = Tstart.X - end.X, 
                            dY = Tstart.Y - end.Y;
                        double Tdis = Math.Sqrt(dX*dX + dY*dY);
                        if (Tdis <= limitDistanceToConnectToOthers) {
                            if (Tdis<disEndToStart) { 
                                nearestS=j; 
                                disEndToStart=Tdis; 
                            }
                        }
                    } 

                    // end to end
                    if (!pTo.End) {
                        // try connect to ending point
                        DIntB Tend=pTo.Points[pTo.Points.Count-1];
                        int dX=Tend.X-end.X, 
                            dY=Tend.Y-end.Y;
                        double Tdis2=Math.Sqrt(dX*dX + dY*dY);
                        if (Tdis2<=limitDistanceToConnectToOthers) {
                            if (Tdis2<disEndToEnd) { 
                                nearestE=j;
                                disEndToEnd=Tdis2; 
                            }
                        }
                    }                       
                }
                
                if (pFrom.End) continue;

                if (nearestS!=-1 && nearestE!=-1) { 
                    if (disEndToStart<disEndToEnd) {
                        CTS();
                    } else {
                        CTE();
                    }
                } else if (nearestS!=-1) { 
                    CTS();
                } else if (nearestS!=-1) { 
                    CTE();
                }

                void CTS() {                    
                    MyPolyline pl=polylines[nearestS];
                    DIntB Tstart=pl.Points[0];
                   
                    pl.Points[0].SetN(true);
                   // pFrom.Points.Add(Tstart);
                    //pFrom.End=true;

                    pFrom.AddClosingEndPoint(Tstart);
                  //  pl.Start=true;
                }
                void CTE() {
                    MyPolyline pl=polylines[nearestE];
                    DIntB Tend=pl.Points[pl.Points.Count-1];
                    //if (Tend.X==pl.GetPointEnd().X && Tend.Y==pl.GetPointEnd().Y) { 
                    //    return;  
                    //}

                    pFrom.AddClosingEndPoint(Tend);
                   // pFrom.Points.Add(Tend);
                    pl.End=true;
                   // pFrom.End=true;
                }    
            }
               
            // Zkus napojit na svou smyËku, nekde doprost¯ed
            foreach (MyPolyline polyline in polylines) {  
                // Uzav¯en· smyËka, ne¯eö 
                if (polyline.Start && polyline.End) continue;

                // Connect start to somewhere
                if (!polyline.Start) { 
                    int result = polyline.NearPointToSelfPointStart(limitDistanceToConnectToBody, (int)connectToBody+1);

                    if (result!=-1) { 
                        DIntB pt=polyline.Points[result];
                        polyline.Points[result].SetN(true);
                        polyline.AddClosingStartPoint(pt);
                        //if (pt.X==polyline.Points[0].X && pt.Y==polyline.Points[0].Y) { 
                        //    polyline.Start=true;
                        //}else{
                        //    polyline.Start=true;
                        //    polyline.Points.Insert(0, pt);
                        //}
                    }
                } 
                
                if (!polyline.End) { 
                    int result = polyline.NearPointToSelfPointEnd(limitDistanceToConnectToBody, (int)connectToBody+1);

                    if (result!=-1) { 
                        DIntB pt=polyline.Points[result];
                        polyline.AddClosingEndPoint(pt);
                        //polyline.Points[result].SetN(true);
                        ////pt.N=true;
                        //if (pt.X==polyline.GetPointEnd().X && pt.Y==polyline.GetPointEnd().Y) { 
                        //    polyline.End=true;
                        //}else{
                        //    polyline.Points.Add(pt);
                        //    polyline.End=true;
                        //}
                    }
                }
            }

            // Zkus napojit na jinou smyËku, nekde doprost¯ed
            foreach (MyPolyline polyline in polylines) {  
                // Uzav¯en· smyËka, ne¯eö 
                if (polyline.Start && polyline.End) continue;

                // Connect start to somewhere
                if (!polyline.Start) { 
                    DIntB start=polyline.Points[0];

                    (MyPolyline, int) result = MyPolyline.NearPointToPoint(polylines, limitDistanceToConnectToBody, start, polyline);
                    if (result.Item1!=null) { 
                        DIntB pt=result.Item1.Points[result.Item2];                        
                        result.Item1.Points[result.Item2].SetN(true);
                        //pt.N=true;
                        polyline.AddClosingStartPoint(pt);
                        //if (pt.X==polyline.Points[0].X && pt.Y==polyline.Points[0].Y) { 
                        //    //break;    
                        //    polyline.Start=true;
                        //}else{
                        //    polyline.Points.Insert(0, pt);
                        //    polyline.Start=true;
                        //}
                    }
                } 
                
                if (!polyline.End) { 
                    DIntB end=polyline.Points[polyline.Points.Count-1];

                    (MyPolyline, int) result = MyPolyline.NearPointToPoint(polylines, limitDistanceToConnectToBody, end, polyline);
                    if (result.Item1!=null) { 
                        DIntB pt=result.Item1.Points[result.Item2];
                        result.Item1.Points[result.Item2].SetN(true);
                        polyline.AddClosingEndPoint(pt);
                       // pt.N=true;
                        //if (pt.X==polyline.GetPointEnd().X && pt.Y==polyline.GetPointEnd().Y) { 
                        ////    break;
                        //    polyline.End=true;
                        //}else{
                        //    polyline.Points.Add(pt);
                        //    polyline.End=true;
                        //}
                    }
                }
            }
             
            
            foreach (MyPolyline pol in polylines) { 
                DIntB lastPoint=pol.Points[0];

                for (int i = 1; i < pol.Points.Count; i++) {
                    DIntB pt=pol.Points[i];
                    if (pt.X==lastPoint.X && pt.Y==lastPoint.Y) { 
                        if (lastPoint.N && pt.N) {
                            pol.Points.RemoveAt(i);
                            i--;
                            continue;
                        }else if (lastPoint.N && !pt.N) {
                            pol.Points.RemoveAt(i);
                            i--;     
                            continue;                       
                        }else if (!lastPoint.N && pt.N) {
                            pol.Points.RemoveAt(i-1);
                            i--;    
                            continue;                        
                        }else if (!lastPoint.N && !pt.N) {
                            pol.Points.RemoveAt(i);
                            i--;   
                            continue;
                        }
                    }
                    lastPoint=pt;
                }
            }

            // Convert
            List<Polyline> pls2 = new List<Polyline>();
            foreach (MyPolyline pol in polylines) {  
                if (ClosedOnly) {
                    if (!pol.Start || !pol.End)continue;
                }
                int len=pol.Points.Count;
                List<Point3d> pnts3d = new List<Point3d>(){Capacity=len}; 
            
                // Symplify polyline       
                DIntB[] sympfifiedPoints;                    
                if (simplify==0) sympfifiedPoints=pol.Points.ToArray();
                else sympfifiedPoints=SimplifyPolyline.Reduce(pol.Points.ToArray(), simplify);

                // NemÏlo by nastat
                if (sympfifiedPoints.Length<=2) sympfifiedPoints=pol.Points.ToArray();

                for (int i = 0; i < sympfifiedPoints.Length; i++) {
                    DIntB pt=sympfifiedPoints[i];
                    pnts3d.Add(new Point3d(pt.X*TransformVector.X+TransformLocation.X, pt.Y*TransformVector.Y+TransformLocation.Y, 0));
                }

                Polyline pl=new Polyline(pnts3d);
                pls2.Add(pl); 
            }
            
            bitmap.UnlockBits(data);
            #if DEBUG
            // edit for output
            {
                BitmapData data2 = bitmap.LockBits(rec, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int Stride=data2.Stride;
                byte* pointerS=(byte*)data.Scan0;
                byte* pointerEdit=pointerS;
                int to=rec.Height*Stride;
                byte* pointerEditTo=pointerEdit+to;

                for (; pointerEdit<pointerEditTo; pointerEdit+=3) {
                    if (*pointerEdit<100) *pointerEdit+=100;

                    if (*(pointerEdit+1)==1) *(pointerEdit+1)=100;
                }

                foreach (MyPolyline pl in polylines) {
                    DIntB start=pl.GetPointStart();
                    SetValueAreaBlue(start.X, start.Y, 4, 200);

                    DIntB end=pl.GetPointEnd();
                    SetValueAreaBlue(end.X, end.Y, 6, 100);
                }

                void SetValueAreaBlue(int x, int y, int radius, byte value) { 
                    for (int iy=-radius; iy<=radius; iy++) {
                        int posY=y+iy;
                        for (int ix=-radius; ix<=radius; ix++) { 
                            if (Math.Sqrt(ix*ix + iy*iy) <= radius) {
                                int posX=x+ix;

                                if (posX<0) continue;
                                if (posY<0) continue;
                                if (posX>=PictureWidth) continue;
                                if (posY>=PictureHeight) continue;

                                *(pointerS + Stride*posY+posX*3+2)=value;
                            }
                        }
                    }
                }  
                
                bitmap.UnlockBits(data2);
            }
            #endif
            try{
                DA.SetDataList(0, pls2);
            }catch{
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Nepoda¯ilo se odeslat data do Grasshopperu\nCannot send data to Grasshopper");
                DA.AbortComponentSolution();
            }
            #if DEBUG
            DA.SetData(1, bitmap);
            #endif
        }
                
        protected override Bitmap Icon => Properties.Resources.detect;

        public override Guid ComponentGuid => new Guid("44712965-93F3-43EE-BDC4-3257A082F521");
    }
}