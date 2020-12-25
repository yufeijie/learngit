function x = solveSRC_wr(P_, Vin_, n_, fs_, Zr_, Td_)
global P Vin n fs Zr Td
P = P_;
Vin = Vin_;
n = n_;
fs = fs_;
Zr = Zr_;
Td = Td_;
opt = optimset('Display', 'off');
x = fsolve(@functionSRC, [Vin/n 2*pi*fs], opt);
end

function F = functionSRC(x)
global Vin fs Zr Td
Vo = x(1); wr = x(2);
F = [A1(Vo, wr)*sin(-wr*Td)+A2(Vo, wr)*sin(wr/fs/2+-wr*Td);
     A1(Vo, wr)*Zr*cos(-wr*Td)+A2(Vo, wr)*Zr*cos(wr/fs/2+-wr*Td)-2*Vin];
end

function re = Vcrm(Vo, wr)
global P n fs Zr
re = Zr*wr*P/(4*n*Vo*fs);
end

function re = A1(Vo, wr)
global Vin n Zr
re = (Vin+n*Vo+Vcrm(Vo, wr))/Zr;
end

function re = A2(Vo, wr)
global Vin n Zr
re = (Vin-n*Vo+Vcrm(Vo, wr))/Zr;
end