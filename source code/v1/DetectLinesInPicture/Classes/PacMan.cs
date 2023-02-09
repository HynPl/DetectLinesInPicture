using System;

namespace DetectLinesInPicture {
    unsafe class PacMan {
        // Aktuální pozice
        public int currentPosX, currentPosY;
        
        // začátek pole barev obrázku
        public static byte* Pointer;

        // Rozměr obrázku
        public static int PictureWidth, PictureHeight;

        // Tloušťka čáry v obrázku
        public static int DistanceMin, DistanceMax;
        public static double DistanceMinD, DistanceMaxD;

        // Šířka obrázku*3
        public static int Stride;

        public PacMan(int x, int y) { 
            SetPosition(x,y);    
        }

        // Nastav pozici PacMana
        public void SetPosition(int x, int y) { 
            currentPosX=x;
            currentPosY=y;
        }
        
        // Najdi nejlepší další bod, kde se přesunout
        public TInt FindNextPoint(bool first) { 
            TInt Best=new TInt(-1, -1, 0);

            // Body v jisté vzdálenosti od aktuální pozice
            for (int iy=-DistanceMax; iy<=DistanceMax; iy++) {
                for (int ix=-DistanceMax; ix<=DistanceMax; ix++) {
                    double dis=Math.Sqrt(ix*ix+iy*iy);
                    if ((dis <= DistanceMax && dis>DistanceMin) || (first && dis <= DistanceMax+1)) {
                        int posX=currentPosX+ix,
                            posY=currentPosY+iy;

                        // Ne mimo obrázek
                        if (posX<0) continue;
                        if (posY<0) continue;
                        if (posX>=PictureWidth) continue;
                        if (posY>=PictureHeight) continue;

                        // Tady už se to řešilo
                        float valueA=GetValue(posX, posY, 1);
                        if (valueA==1) continue;
                                               
                        // Nejlepší hodnota, kterou se řídit
                        int valueN=GetValue(posX, posY, 0);
                        if (valueN==0) continue;

                        // hodnota valueA==2 znamená že jsme objely kolečko a vrátili jsme se na začátek
                        if (valueA==2) { 
                             TInt tint=new TInt{ X=posX, Y=posY, N=-2 };
                        
                            if (Best.N<tint.N) Best=tint;
                        } else {
                            // Je-li lepší jak předchozí 
                            if (Best.N<valueN) Best=new TInt{ X=posX, Y=posY, N=valueN };
                        }
                    }
                }
            }

            // Vrať to nejlepší
            return Best;
        }

        // Získej hodnotu barvy pixelu, s upřesněním chanelu
        public int GetValue(int x, int y, int ch) => *(Pointer + y*Stride+x*3+ch);

        // Nastav hodnotu barvy pixelu, s upřesněním chanelu
        public void SetValue(int x, int y, int ch, byte value) => *(Pointer + Stride*y+x*3+ch)=value;
       
        // Nastav barvu pro oblast v zeleném
        public static int SetValueArea1(int x, int y, int radius, byte value) { 
            for (int ix=-radius; ix<=radius; ix++) { 
                for (int iy=-radius; iy<=radius; iy++) {
                    double dis=Math.Sqrt(ix*ix+iy*iy);
                    if (dis <= radius) {
                        int posX=x+ix, posY=y+iy;

                        // Ne mimo obrázek
                        if (posX<0) continue;
                        if (posY<0) continue;
                        if (posX>=PictureWidth) continue;
                        if (posY>=PictureHeight) continue;

                        // Nastav barvu
                        *(Pointer + Stride*posY+posX*3+1)=value;
                    }
                }
            }
            return value;
        }

        // Nastav barvu pro oblast v modrém
        public static int SetValueArea2(int x, int y, int radius, byte value) { 
            for (int ix=-radius; ix<=radius; ix++) { 
                for (int iy=-radius; iy<=radius; iy++) {
                    double dis=Math.Sqrt(ix*ix+iy*iy);
                    if (dis <= radius) {
                        int posX=x+ix, posY=y+iy;

                        // Ne mimo obrázek
                        if (posX<0) continue;
                        if (posY<0) continue;
                        if (posX>=PictureWidth) continue;
                        if (posY>=PictureHeight) continue;

                        // Nastav barvu
                        *(Pointer + Stride*posY+posX*3+2)=value;
                    }
                }
            }
            return value;
        }
    }
}