function plotPareto3DOnly(headline, subTitle1, subTitle2, subTitle3, subTitle4, xLabelName, yLabelName, zLabelName, dataPareto)
% plotPareto ��Paretoͼ
% headline �����
% subTitle1 2 3 4 �ӱ���
% xLabelName yLabelName zLabelName ��������
% size ��������
% data  ����
% dataPareto Pareto��������
sz = 12;

figure(9)

scatter3(dataPareto(:, 1), dataPareto(:, 2), dataPareto(:, 3), sz, getCmap(dataPareto(:, 7), 10, 100),  'filled');
grid on
title(subTitle1)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)

end

function cmap = getCmap(c, min, max)
shift = 0.4;

n = length(c);
cmap = zeros(n, 3);
for i = 1 : n
    normal = (c(i)-min)/(max-min);
    delta = (1-shift)*normal;
    cmap(i, 1) = delta;
    cmap(i, 2) = shift+delta;
    cmap(i, 3) = shift+delta;
end
end