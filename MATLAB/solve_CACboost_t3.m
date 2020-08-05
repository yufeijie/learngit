function x = solve_CACboost_t3(Vo_, Vcc_, IL0_, ILr3_, fs_, Lr_, Cr_, nI_)
global Vo Vcc IL0 ILr3 Ts Lr Zr wr nI
Vo = Vo_;
Vcc = Vcc_;
IL0 = IL0_;
ILr3 = ILr3_;
Ts = 1/fs_;
Lr = Lr_;
wr = 1/sqrt(Lr_*Cr_);
Zr = sqrt(Lr_/Cr_);
nI = nI_;

opt = optimset('Algorithm','levenberg-marquardt', 'Display', 'off');
x = fsolve(@functionSRC, Ts, opt);
end

function F = functionSRC(x)
global Vo Vcc wr
t3 = x(1);
F = t3-acos(-Vcc/Vo)/wr-t2(t3);
end

function re = t2(t3)
global Vo Lr
re = Lr/Vo*ILr1(t3)+t1(t3);
end

function re = ILr1(t3)
global IL0 wr Zr nI
re = nI*IL0-A0(t3)/Zr*cos(wr*t1(t3)+a0(t3));
end

function re = t1(t3)
global Vo wr
re = (pi-asin(-Vo/A0(t3))-a0(t3))/wr;
end

function re = A0(t3)
global Vcc IL0 Zr nI
re = sqrt(Vcc^2+(Zr*(nI*IL0-ILr0(t3)))^2);
end

function re = a0(t3)
global Vcc IL0 Zr nI
re = atan(Vcc/(Zr*(nI*IL0-ILr0(t3))));
if sin(re) < 0
    re = re+pi;
end
end

function re = ILr0(t3)
global Vcc ILr3 Ts Lr
re = ILr3+Vcc/Lr*(Ts-t3);
end
