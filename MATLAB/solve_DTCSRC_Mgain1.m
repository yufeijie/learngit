function x = solve_DTCSRC_Mgain1(Td_, fs_, Q_)
global Td fs Q
Td = Td_;
fs = fs_;
Q = Q_;
opt = optimset('Display', 'off');
x = fsolve(@functionSRC, 10, opt);
end

function F = functionSRC(x)
global Td fs Q
M = x(1);
F = fgainccm(Td, fs, Q, M);
end

function re = fgainccm(Td, fs, Q, M)
re = (Vcrpk(Td, fs, Q, M)+1)^2+M^2+M*(Vcrpk(Td, fs, Q, M)+1)*(1-cos(2*pi*Td/fs))+(Vcrpk(Td, fs, Q, M)+1+M)*sqrt((Vcrpk(Td, fs, Q, M)+1)^2+M^2-2*M*(Vcrpk(Td, fs, Q, M)+1)*cos(2*pi*Td/fs))*cos(pi/fs-2*pi*Td/fs+BP(Td, fs, Q, M))-2;
end

function re = BP(Td, fs, Q, M)
re = atanx((Vcrpk(Td, fs, Q, M)+1)*sin(2*pi*Td/fs)/((Vcrpk(Td, fs, Q, M)+1)*cos(2*pi*Td/fs)-M));
end

function re = Vcrpk(Td, fs, Q, M)
re = (1-cos(2*pi*Td/fs)+pi/fs*Q*M)/(1+cos(2*pi*Td/fs));
end

function re = atanx(x)
re = atan(x);
if re < 0
    re = re+pi;
end
end