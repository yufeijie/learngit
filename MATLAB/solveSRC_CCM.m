function x = solveSRC_CCM(P_, Vin_, n_, fs_, wr_, Zr_, Td_)
global P Vin n fs Zr wr
P = P_;
Vin = Vin_;
n = n_;
fs = fs_;
wr = wr_;
Zr = Zr_;
opt = optimset('Display', 'off');
x = fsolve(@functionSRC, [Vin/n -wr*Td_], opt);
end

function F = functionSRC(x)
global Vin fs Zr wr
Vo = x(1); fy = x(2);
F = [A1(Vo)*sin(fy)+A2(Vo)*sin(wr/fs/2+fy);
     A1(Vo)*Zr*cos(fy)+A2(Vo)*Zr*cos(wr/fs/2+fy)-2*Vin];
end

function re = Vcrm(Vo)
global P n fs wr Zr
re = Zr*wr*P/(4*n*Vo*fs);
end

function re = A1(Vo)
global Vin n Zr
re = (Vin+n*Vo+Vcrm(Vo))/Zr;
end

function re = A2(Vo)
global Vin n Zr
re = (Vin-n*Vo+Vcrm(Vo))/Zr;
end