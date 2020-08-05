function plotMesh(titleName, xLabelName, yLabelName, zLabelName, x, y, z)
% plotMesh ����ά����ͼ
% titleName ������
% xLabelName X����
% yLabelName Y����
% zLabelName Z����
% x y z ����
figure(2)
mesh(x, y, z);
colormap(flipud(jet));
colorbar;
shading interp;
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end