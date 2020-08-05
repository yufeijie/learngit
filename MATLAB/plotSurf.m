function plotSurf(titleName, xLabelName, yLabelName, zLabelName, x, y, z)
% plotSurf ����ά����ͼ����JET��
% titleName ������
% xLabelName X����
% yLabelName Y����
% zLabelName Z����
% x y z ����
figure(3)
surf(x, y, z);
colormap(flipud(jet));
colorbar;
shading interp;
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end