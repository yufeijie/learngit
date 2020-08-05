function x = solve_DTCSRC_Mgain2(Td_, fs_, Q_)
global Td fs Q
Td = Td_;
fs = fs_;
Q = Q_;
opt = optimset('Display', 'off');
x = fsolve(@functionSRC, 4, opt);
end

function F = functionSRC(x)
global Td fs Q
M = x(1);
F = fgaindcm(Td, fs, Q, M);
end

function re = fgaindcm(Td, fs, Q, M)
re = 2*(Vcrpk(Td, fs, Q, M)+1)+2*M+-(Vcrpk(Td, fs, Q, M)+1)*M*(1+cos(2*pi*Td/fs))-2;
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