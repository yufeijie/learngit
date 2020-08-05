function plotSurf2(titleName, xLabelName, yLabelName, zLabelName, x, y, z)
% plotSurf2 ����ά����ͼ
% titleName ������
% xLabelName X����
% yLabelName Y����
% zLabelName Z����
% x y z ����
figure(4)
surf(x, y, z);
colormap(jet);
colorbar;
shading interp;
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end